using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;


namespace ImprovedExperience
{


    public class GTAExtended : Script
    {
        string ScriptName = "GTAExtended-Gameplay";
        string ScriptVer = "0.3";

        bool RightExit = true;
        bool OldGTAEfound = false;

        //Effects
        bool CanGrab = true;
        bool CanHook = true;
        bool VehicleInteractionsFoot = true;

        bool SlideHelper = true;
        public GTAExtended()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            if (File.Exists(@"scripts\\GTAExtended.dll")) OldGTAEfound = true;

            LoadSettings();
        }


        int GameTimeRef = Game.GameTime;


        Prop haxprop = null;
        Entity grabbedent = null;

        public static Vector3 LerpByDistance(Vector3 A, Vector3 B, float x)
        {
            Vector3 P = x * Vector3.Normalize(B - A) + A;
            return P;
        }

        int HandbrakeCooldown = Game.GameTime;
        bool HandbrakeOn = false;
        List<string> tutorial = new List<string>();

        void HandleInteractions()
        {
            string text = "~BLIP_INFO_ICON~ You can attach and detach cargo to your vehicle by pressing ~INPUT_DETONATE~.";
            if (!tutorial.Contains(text) && CanHook && Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, Game.Player.Character, 164))
            {

                //DisplayHelpTextThisFrame(text);
                MessageQueue.Add(text);
                MessageQueue.Add("~BLIP_INFO_ICON~ The cargo must be behind your vehicle.");

                tutorial.Add(text);
                return;
            }
            text = "~BLIP_INFO_ICON~ You can fix an engine if you have a wrench.";
            if (!tutorial.Contains(text) && CanWeUse(Game.Player.Character.CurrentVehicle))
            {
                if (Game.Player.Character.CurrentVehicle.EngineHealth < 800)
                {
                    MessageQueue.Add(text);
                    MessageQueue.Add("Just press ~INPUT_CONTEXT~ while near the engine.");
                    tutorial.Add(text);
                }
            }
            text = "~BLIP_INFO_ICON~ You can now leave the car by the right side. ";
            if (!tutorial.Contains(text) && Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, Game.Player.Character, 168))
            {

                //DisplayHelpTextThisFrame(text);
                MessageQueue.Add(text);
                tutorial.Add(text);
                MessageQueue.Add("Just press ~INPUT_SPRINT~ + ~INPUT_VEH_EXIT~ when you leave your vehicle.");// by turning right while pressing .

            }

            Vehicle v = Game.Player.Character.CurrentVehicle;
            if (CanWeUse(v))
            {

                if (CanHook && Game.IsControlJustPressed(2, GTA.Control.Detonate))
                {
                    if (!CanWeUse(GetAttachedVehicle(v, true)) && v.Speed < 1f)
                    {
                        Attach(v, null);
                        return;
                    }
                }

                if (RightExit && Game.IsControlJustPressed(2, GTA.Control.VehicleExit) && v.Velocity.Length() < 4f)
                {
                    if (Game.IsControlPressed(2, GTA.Control.Sprint))
                    {
                        Function.Call(Hash.TASK_LEAVE_VEHICLE, Game.Player.Character, v, 262144);
                    }
                }

                if (HandbrakeCooldown < Game.GameTime)
                {
                    if (Game.IsControlJustPressed(2, GTA.Control.VehicleHandbrake) && !HandbrakeOn && v.Velocity.Length() < 1f && v.Model.IsCar)
                    {
                        HandbrakeOn = true;
                        UI.Notify("Handbrake enabled.");
                        v.HandbrakeOn = true;
                        HandbrakeCooldown = Game.GameTime + 400;
                    }

                    if (Game.IsControlJustReleased(2, GTA.Control.VehicleHandbrake) && HandbrakeOn)
                    {
                        UI.Notify("Handbrake disabled.");
                        v.HandbrakeOn = false;
                        HandbrakeOn = false;
                    }

                }
            }

            if (Game.Player.Character.IsOnFoot)
            {

                if (Game.Player.Character.IsInMeleeCombat)
                {
                    if (CanWeUse(haxprop))
                    {
                        haxprop.Delete();

                        if (CanWeUse(grabbedent))
                        {
                            grabbedent.ApplyForce(((Game.Player.Character.ForwardVector * 5) * Game.Player.Character.Velocity.Length()) + ((Game.Player.Character.UpVector * 6)));

                        }
                    }
                }

                if (Game.IsControlJustReleased(2, GTA.Control.Context))
                {

                    //  UI.Notify("grab");
                    //Drop weapon
                    /*
                if(Game.Player.Character.Weapons.Current.Hash != WeaponHash.Unarmed)
                {
                    Vector3 d = Game.Player.Character.Position.Around(5);
                    Function.Call(Hash.SET_PED_DROPS_INVENTORY_WEAPON, Game.Player.Character, (int)Game.Player.Character.Weapons.Current.Hash, d.X,d.Y,d.Z, 5);
                }            
                         */

                    Vehicle lastv = Game.Player.Character.LastVehicle;


                    if (CanWeUse(haxprop))
                    {
                        haxprop.Delete();
                        if (CanWeUse(grabbedent))
                        {
                            if (Game.Player.Character.Velocity.Length() < 2f)
                            {
                                grabbedent.Rotation = Game.Player.Character.Rotation;

                                //Vector3 pos = Game.Player.Character.Position + Game.Player.Character.ForwardVector;
                                // Function.Call<bool>(Hash.SLIDE_OBJECT, grabbedent, pos.X, pos.Y, pos.Z, 1f, 1f, 1f, true);

                                grabbedent.Position = Game.Player.Character.Position + Game.Player.Character.ForwardVector;
                            }
                            else
                            {
                                grabbedent.ApplyForce(((Game.Player.Character.ForwardVector * 5) * Game.Player.Character.Velocity.Length()) + ((Game.Player.Character.UpVector * 6)));

                            }

                            grabbedent.IsPersistent = false;

                            grabbedent.SetNoCollision(Game.Player.Character, false);
                        }
                        grabbedent = null;

                        return;
                    }
                    else if (VehicleInteractionsFoot && CanWeUse(lastv) && Game.Player.Character.IsInRangeOf(lastv.Position, 4f))
                    {
                        if (lastv.HasBone("boot") && Game.Player.Character.IsInRangeOf(lastv.GetBoneCoord("boot"), 2f))
                        {
                            if (lastv.IsDoorOpen(VehicleDoor.Trunk))
                            {
                                lastv.CloseDoor(VehicleDoor.Trunk, false);
                            }
                            else
                            {
                                lastv.OpenDoor(VehicleDoor.Trunk, false, false);
                            }
                        }
                        else if (lastv.HasBone("bonnet") && Game.Player.Character.IsInRangeOf(lastv.GetBoneCoord("bonnet"), 2f))
                        {
                            if (lastv.IsDoorOpen(VehicleDoor.Hood))
                            {
                                if ((int)Game.Player.Character.Weapons.Current.Hash == Game.GenerateHash("WEAPON_WRENCH"))
                                {

                                    if (lastv.EngineHealth < 1)
                                    {
                                        UI.Notify("~b~" + lastv.FriendlyName + "~w~'s engine partially fixed. It will start up, at least.");
                                        lastv.EngineHealth = 1;
                                    }
                                    else
                                    {
                                        UI.Notify("~b~" + lastv.FriendlyName + "~w~'s engine is already working.");

                                    }
                                }
                                else
                                {
                                    lastv.CloseDoor(VehicleDoor.Hood, false);
                                }
                            }
                            else
                            {
                                lastv.OpenDoor(VehicleDoor.Hood, false, false);
                            }
                        }
                        return;
                    }
                    else if (Function.Call<bool>(Hash._0x7C2AC9CA66575FBF, Game.Player.Character) && CanGrab)
                    {
                        foreach (Entity ent in World.GetNearbyEntities(Game.Player.Character.Position, 3f))
                        {
                            if (ent != Game.Player.Character && !ent.IsAttached())
                            {
                                int d = 28422;
                                Vector3 pos = ent.Position;
                                if (Function.Call<Vector3>(Hash.GET_OFFSET_FROM_ENTITY_GIVEN_WORLD_COORDS, Game.Player.Character, pos.X, pos.Y, pos.Z).Y > 0)
                                {
                                    d = 28422;
                                }
                                else
                                {
                                    d = 60309;
                                }
                                d = Function.Call<int>(Hash.GET_PED_BONE_INDEX, Game.Player.Character, d);

                                haxprop = World.CreateProp("prop_candy_pqs", Game.Player.Character.Position, true, true);
                                Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, haxprop, Game.Player.Character, d, 0f, 0f, 0f, 0f, 0f, 0f, true, true, false, true, 0, true); //+ (v.Model.GetDimensions().Y/2f)

                                haxprop.Alpha = 0;

                                float dist = 100f;
                                int bone = 0;

                                if (ent.Model.IsPed)
                                {

                                    if (Function.Call<int>(Hash.GET_PED_BONE_INDEX, ent, (int)Bone.SKEL_Head) != -1) bone = Function.Call<int>(Hash.GET_PED_BONE_INDEX, ent, (int)Bone.SKEL_Head);
                                }
                                else if (!ent.Model.IsVehicle)
                                {
                                    for (int i = -1; i < 10000; i++)
                                    {
                                        float distto = Function.Call<Vector3>(Hash.GET_WORLD_POSITION_OF_ENTITY_BONE, ent, Function.Call<int>(Hash.GET_PED_BONE_INDEX, ent, i)).DistanceTo(Function.Call<Vector3>(Hash.GET_WORLD_POSITION_OF_ENTITY_BONE, Game.Player.Character, d));
                                        if (distto < dist)
                                        {
                                            bone = Function.Call<int>(Hash.GET_PED_BONE_INDEX, ent, i);
                                            dist = distto;
                                        }
                                    }
                                }
                                grabbedent = ent;
                                grabbedent.IsPersistent = true;

                                Function.Call(Hash.SET_ENTITY_LOAD_COLLISION_FLAG, grabbedent, true);

                                if (bone < 0) bone = 0;

                                float zOffset = 0f;

                                //if (bone == 0) zOffset = ent.Model.GetDimensions().Z / 2;
                                Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY_PHYSICALLY, ent, haxprop, bone, 0, 0f, 0f, 0f, 0f, 0f, -zOffset, 0f, 0f, 0f, 10000f, false, true, true, true, 2); //+ (v.Model.GetDimensions().Y/2f)
                                ent.SetNoCollision(Game.Player.Character, true);

                                if (ent.Model.IsPed)
                                {
                                    Function.Call(Hash.SET_PED_TO_RAGDOLL, ent, -1, -1, 3, true, true, true);
                                    Function.Call(Hash.CREATE_NM_MESSAGE, 1151);
                                    Function.Call(Hash.GIVE_PED_NM_MESSAGE, ent, true);
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }


        public static void DrawLine(Vector3 from, Vector3 to)
        {
            Function.Call(Hash.DRAW_LINE, from.X, from.Y, from.Z, to.X, to.Y, to.Z, 255, 255, 0, 255);
        }
        int fxhandle = -1;


        string Sparks = "weap_veh_turbulance_sand"; // "ent_dst_dust";
        int Sand = -1;
        int PlaneLanded = -1;
        string core = "scr_recartheft";
        string ptfx = "scr_wheel_burnout";
        public static List<int> WheelBoneLanded = new List<int>();
        public static List<string> WheelBoneRef = new List<string> { "wheel_lf", "wheel_rf", "wheel_lm", "wheel_rm", "wheel_lr", "wheel_rr", };

        int Camera = 0;


        //    List<string> Messages = new List<string>();
        /*
            void HandleQueuedHelp() //ontick
            {
                if (Messages.Count > 0)
                {
                    {
                        DisplayHelpTextThisFrame(Messages[0]);
                        Messages.RemoveAt(0);
                    }
                }
            }
            */
        Camera LookBehind = World.CreateCamera(Vector3.Zero, Vector3.Zero, GameplayCamera.FieldOfView);
        Camera Temp = World.CreateCamera(Vector3.Zero, Vector3.Zero, GameplayCamera.FieldOfView);

        void OnTick(object sender, EventArgs e)
        {
            if (LookBehind == World.RenderingCamera)
            {
                LookBehind.Rotation = Game.Player.Character.CurrentVehicle.Rotation + new Vector3(180, 170, -20);
            }
            if (Game.IsControlPressed(2, GTA.Control.Sprint) || World.RenderingCamera == LookBehind) Game.DisableControlThisFrame(2, GTA.Control.VehicleLookBehind);

            if (Game.IsControlPressed(2, GTA.Control.Sprint))
            {
                if (Game.IsControlJustPressed(2, GTA.Control.VehicleLookBehind) && World.RenderingCamera!= LookBehind)
                {
                    if (Function.Call<int>(Hash.GET_FOLLOW_VEHICLE_CAM_VIEW_MODE) == 4 && CanWeUse(Game.Player.Character.CurrentVehicle))
                    {
                        Vehicle V = Game.Player.Character.CurrentVehicle;
                        Vector3 offset = Game.Player.Character.GetBoneCoord(Bone.IK_Head);
                        offset = Function.Call<Vector3>(Hash.GET_OFFSET_FROM_ENTITY_GIVEN_WORLD_COORDS, V, offset.X, offset.Y, offset.Z);

                        if (LookBehind != World.RenderingCamera)
                        {
                            World.RenderingCamera = Temp;


                            Temp.Rotation = V.Rotation;
                            Temp.AttachTo(V, offset + new Vector3(0, 0, 0.1f));// new Vector3(-V.Model.GetDimensions().X / 2, 0, 0.5f));

                            //Temp.Position = V.Position + new Vector3(-V.Model.GetDimensions().X / 3, 0, 0.5f);
                            //LookBehind.Position = GameplayCamera.Position-(new Vector3(-0.5f,0,0));
                            LookBehind.Rotation = V.Rotation + new Vector3(180, 170, -20); //new Vector3(V.Rotation.X, V.Rotation.Y, V.Rotation.Z);
                            LookBehind.AttachTo(V, offset + new Vector3(-0.6f, V.Speed * 0.03f, 0));// new Vector3(-V.Model.GetDimensions().X/2,0,0.5f));
                                                                                                       //Temp.InterpTo(LookBehind, 2000, true, true);

                            if (V.Velocity.Length() < 1f) Function.Call(Hash.SET_CAM_ACTIVE_WITH_INTERP, LookBehind, Temp, 500, 1, 1);
                            else World.RenderingCamera = LookBehind;
                        }
                    }

                }
            }
            if (Game.IsControlJustReleased(2, GTA.Control.VehicleLookBehind) && LookBehind == World.RenderingCamera)
            {
                World.RenderingCamera = null;
            }
            if (WasCheatStringJustEntered("touch"))
            {
                Function.Call(Hash.SET_VEHICLE_OUT_OF_CONTROL, Game.Player.Character.CurrentVehicle, false, false);
            }
            if (WasCheatStringJustEntered("remove"))
            {
                Game.Player.Character.CurrentVehicle.Repair();
            }
            if (OldGTAEfound)
            {
                DisplayHelpTextThisFrame("~b~" + ScriptName + "~w~ disabled, old GTAExtended.dll file found. Delete it.");
                return;
            }

            /*
            if (WasCheatStringJustEntered("touch"))
            {
                //Game.Player.Character.CurrentVehicle.IsDriveable = false;
                string d = "";
                for(int i =0; i<30; i++)
                {
                    Game.Player.Character.CurrentVehicle.ToggleMod((VehicleToggleMod)i, true);

                     d+= " "+Function.Call<int>(Hash.IS_TOGGLE_MOD_ON, Game.Player.Character.CurrentVehicle, i).ToString();
                }

                UI.Notify(d);
            }*/

            HandleMessages();
            // HandleQueuedHelp();
            HandleInteractions();
            HandleRebound();
            if (Game.Player.Character.Velocity.Length() < 2f && CanWeUse(grabbedent) && Game.IsControlPressed(2, GTA.Control.Context))
            {
                World.DrawMarker(MarkerType.UpsideDownCone, Game.Player.Character.Position + Game.Player.Character.ForwardVector + new Vector3(0, 0, 0.4f), Vector3.Zero, Vector3.Zero, new Vector3(0.3f, 0.3f, 0.3f), Color.DeepSkyBlue);
            }
            if (WasCheatStringJustEntered("fx"))
            {
                if (Function.Call<bool>(Hash.DOES_PARTICLE_FX_LOOPED_EXIST, fxhandle))
                {
                    Function.Call(Hash.REMOVE_PARTICLE_FX, fxhandle);
                    UI.ShowSubtitle("Fx removed.");
                }
                else
                {
                    UI.ShowSubtitle("~b~Input the PTFX Asset.");

                    string name = Game.GetUserInput(40);
                    Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, name);
                    Script.Wait(200);
                    if (Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, name))
                    {
                        UI.ShowSubtitle("~b~Input the PTFX name.");
                        string fxname = Game.GetUserInput(40);
                        Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, name);
                        fxhandle = Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_ON_ENTITY, fxname, Game.Player.Character, 0, 0, 0, 0, 0, 0, 3f, true, true, true);// Function.Call<int>(Hash._START_PARTICLE_FX_LOOPED_ON_ENTITY_BONE, fxname, Game.Player.Character, 0, 0, 0, 0, 0, 0, 0, 1f, true, true, true);
                        Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, name);

                        int nonloop = Function.Call<int>(Hash.START_PARTICLE_FX_NON_LOOPED_ON_ENTITY, fxname, Game.Player.Character, 0, 0, 0, 0, 0, 0, 3f, true, true, true);// Function.Call<int>(Hash._START_PARTICLE_FX_LOOPED_ON_ENTITY_BONE, fxname, Game.Player.Character, 0, 0, 0, 0, 0, 0, 0, 1f, true, true, true);
                        Script.Wait(500);
                        if (Function.Call<bool>(Hash.DOES_PARTICLE_FX_LOOPED_EXIST, fxhandle))
                        {
                            UI.ShowSubtitle("~b~Fx started.");
                        }
                        else
                        {
                            if (Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, name))
                            {
                                UI.ShowSubtitle("~o~Fx not started but asset is loaded.");
                            }
                            else
                            {
                                UI.ShowSubtitle("~o~Fx not started, asset not loaded");
                            }
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("~o~Asset has not loaded.");
                    }

                }
                /*
                int _START_PARTICLE_FX_LOOPED_ON_ENTITY_BONE(char * effectName, Entity entity, float xOffset, float yOffset, float zOffset, 
                float xRot, float yRot, float zRot, int boneIndex, float scale, BOOL xAxis, BOOL yAxis, BOOL zAxis)
                    */
            }
            if (WasCheatStringJustEntered("dmg")) Game.Player.Character.ApplyDamage(int.Parse(Game.GetUserInput(5)));
            if (WasCheatStringJustEntered("GTAEFix"))
            {
                if (Function.Call<bool>(Hash._GET_SCREEN_EFFECT_IS_ACTIVE, "FocusOut")) Function.Call(Hash._STOP_SCREEN_EFFECT, "FocusOut");
                if (Function.Call<bool>(Hash._GET_SCREEN_EFFECT_IS_ACTIVE, "DeathFailOut")) Function.Call(Hash._STOP_SCREEN_EFFECT, "DeathFailOut");
                if (Function.Call<bool>(Hash._GET_SCREEN_EFFECT_IS_ACTIVE, "HeistCelebPassBW")) Function.Call(Hash._STOP_SCREEN_EFFECT, "HeistCelebPassBW");
                if (Function.Call<bool>(Hash._GET_SCREEN_EFFECT_IS_ACTIVE, "FocusIn")) Function.Call(Hash._STOP_SCREEN_EFFECT, "FocusIn");

            }

            Vehicle v = Game.Player.Character.CurrentVehicle;


            if (CanWeUse(v))
            {
                //DrawLine(v.Position,v.Position+ Function.Call<Vector3>(Hash.GET_ENTITY_SPEED_VECTOR, v, false));
            }
            if (WasCheatStringJustEntered("screenfx"))
            {

                string d = Game.GetUserInput(40);
                Function.Call(Hash._START_SCREEN_EFFECT, d, 2000, false);

                Script.Wait(5000);
                Function.Call(Hash._STOP_SCREEN_EFFECT, d);
            }

            if (SlideHelper && CanWeUse(v) && v.IsOnAllWheels && Math.Abs(Function.Call<Vector3>(Hash.GET_ENTITY_SPEED_VECTOR, v, true).X) > 1)
            {
                float spd = Math.Abs(Function.Call<Vector3>(Hash.GET_ENTITY_SPEED_VECTOR, v, true).X) * 0.2f;

                if (spd < 1) spd = 1;
                if (spd > 1.5f) spd = 1.5f;
                v.EngineTorqueMultiplier = spd;
                // UI.ShowSubtitle("~r~"+spd.ToString());
            }

            // if (CanWeUse(grabbedent)) grabbedent.ApplyForce(Vector3.WorldUp, Vector3.Zero, ForceType.MaxForceRot2);           



            if (CanWeUse(v) && 1 == 2)
            {
                if (v.IsOnAllWheels && Math.Abs(Function.Call<float>(Hash.GET_ENTITY_PITCH, v)) > 5 && !v.IsStopped)
                {
                    float mult = 1;
                    float modifier = Function.Call<float>(Hash.GET_ENTITY_PITCH, v) * 0.02f;

                    if (Function.Call<float>(Hash.GET_ENTITY_PITCH, v) < 0)
                    {
                        if (v.Acceleration >= 0)
                        {
                            mult -= modifier;
                        }
                        else
                        {
                            mult += modifier;
                        }
                    }
                    else
                    {
                        if (v.Acceleration >= 0)
                        {
                            mult -= modifier;
                        }
                        else
                        {
                            mult += modifier;
                        }
                    }
                    mult = (float)Math.Round(mult, 2);
                    if (mult < 0.1f) mult = 0.1f;
                    if (mult > 2) mult = 2;

                    Game.Player.Character.CurrentVehicle.EngineTorqueMultiplier = mult;
                    //DisplayHelpTextThisFrame(Math.Round(Function.Call<float>(Hash.GET_ENTITY_PITCH, v),1).ToString()+" - "+ mult.ToString());
                }
            }

        }



        public static float HeightAboveGround(Vector3 pos)
        {
            //Vector3 pos = v.Position + (v.UpVector * 4f); ;

            RaycastResult cast = World.Raycast(pos, pos + new Vector3(0, 0, -1000), IntersectOptions.Map);


            return cast.HitCoords.DistanceTo(pos);
        }

        float zHeight = 0f;
        float zSpeed = 0f;
        float force = 0;
        bool Jumping = false;
        void HandleRebound()
        {
            return;
            Vehicle v = Game.Player.Character.CurrentVehicle;
            if (CanWeUse(v))
            {

                if (Jumping)
                {
                    if (Function.Call<Vector3>(Hash.GET_ENTITY_SPEED_VECTOR, v, true).Z > 0) v.ApplyForce(v.UpVector * 0.9f, Vector3.Zero, ForceType.MaxForceRot2);
                    if (!v.IsOnAllWheels) Jumping = false;
                }
                else
                {
                    if (v.IsOnAllWheels)
                    {

                        if (v.HeightAboveGround < zHeight - 0.2f && Math.Abs(Function.Call<Vector3>(Hash.GET_ENTITY_SPEED_VECTOR, v, true).Y) > 10f)
                        {
                            float NewzSpeed = Function.Call<Vector3>(Hash.GET_ENTITY_SPEED_VECTOR, v, true).Z;
                            if (zSpeed < 0 && NewzSpeed > -0.1f && NewzSpeed > zSpeed + 0.5f)
                            {
                                force = (Math.Abs(zSpeed) * 0.4f);
                                if (force > 5) force = 5f;
                                UI.ShowSubtitle("~r~Boink " + Math.Round(force, 1), 500);
                                Jumping = true;
                            }
                        }
                    }
                    else
                    {
                        zHeight = v.HeightAboveGround;
                        zSpeed = Function.Call<Vector3>(Hash.GET_ENTITY_SPEED_VECTOR, v, true).Z;
                        if (zSpeed > -4f) zSpeed = 0;
                    }
                }
            }
        }



        Vehicle GetAttachedVehicle(Vehicle carrier, bool DetachIfFound)
        {
            Vehicle Carried = null;
            Vehicle OriginalCarrier = carrier;


            if (carrier.Model.IsCar && carrier.HasBone("attach_female") && carrier.Model != "ramptruck2" && carrier.Model != "ramptruck")
            {
                //ui.notify("Carrier " + carrier.FriendlyName + " has an 'attach_female' bone, looking for trailers");

                if (Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER, carrier))
                {
                    //ui.notify("This carrier has a trailer, getting trailer");
                    Vehicle trailer = null; // GetTrailer(ToCarry);
                    if (trailer == null)
                    {
                        foreach (Vehicle t in World.GetNearbyVehicles(carrier.Position, 30f))
                            if (t.HasBone("attach_male"))
                            {
                                trailer = t;
                                break;
                            }
                    }

                    if (trailer != null)
                    {
                        carrier = trailer;
                        //ui.notify("Trailer found, " + carrier.FriendlyName + "(" + carrier.DisplayName + ")");

                        foreach (Vehicle veh in World.GetNearbyVehicles(carrier.Position, 10f))
                            if (veh != OriginalCarrier && veh != carrier && veh.IsAttachedTo(carrier))
                            {
                                if (DetachIfFound) Detach(carrier, veh);
                                return veh;
                                Carried = veh;
                                //ui.notify("Found ToCarry, " + ToCarry.FriendlyName);
                                break;
                            }
                    }
                    else
                    {
                        //ui.notify("Trailer not found, aborting");
                        return null;
                    }
                }
                else
                {
                    //ui.notify("This carrier doesn't have trailer, aborting");
                    return null;
                }

            }
            else
            {
                //ui.notify("Carrier " + carrier.FriendlyName + " does ~o~NOT~w~ have an 'attach_female' bone, must be a normal car");
                foreach (Vehicle v in World.GetNearbyVehicles(carrier.Position, carrier.Model.GetDimensions().Y))
                {
                    if (v.IsAttachedTo(carrier))
                    {

                        if (DetachIfFound) Detach(carrier, v);

                        return v;
                    }
                }
            }
            return null;
        }

        void Detach(Vehicle carrier, Vehicle cargo)
        {
            cargo.Detach();
            if (carrier == Game.Player.Character.CurrentVehicle) UI.Notify("Detaching " + cargo.FriendlyName + " from " + carrier.FriendlyName);

            if (CanWeUse(Game.Player.Character.CurrentVehicle) && carrier == Game.Player.Character.CurrentVehicle)
                if (Game.IsControlPressed(2, GTA.Control.ParachuteTurnLeftOnly))
                {
                    //ui.notify("~o~Left");

                    cargo.Position = carrier.Position - (carrier.RightVector * carrier.Model.GetDimensions().X);
                }
            if (Game.IsControlPressed(2, GTA.Control.ParachuteTurnRightOnly))
            {
                //ui.notify("~o~Right");
                cargo.Position = carrier.Position + (carrier.RightVector * carrier.Model.GetDimensions().X);
            }
            if (Game.IsControlPressed(2, GTA.Control.ParachutePitchDownOnly))
            {
                //ui.notify("~o~Back");

                cargo.Position = carrier.Position + -(carrier.ForwardVector * carrier.Model.GetDimensions().Y);
                // ToCarry.Position = carrier.Position + (carrier.RightVector * carrier.Model.GetDimensions().X);
            }
        }
        void Attach(Vehicle carrier, Vehicle ToCarry)
        {
            //Vehicle ToCarry = null; // Game.Player.Character.CurrentVehicle;
            if (!CanWeUse(carrier)) return;
            Vehicle OriginalCarrier = carrier;
            bool Finished = false;

            if (!CanWeUse(ToCarry))
            {
                //Cars within this car
                if (carrier.Model.IsCar)
                {
                    foreach (Vehicle v in World.GetNearbyVehicles(carrier.Position, carrier.Model.GetDimensions().Y))
                    {
                        if (v.Handle != carrier.Handle && Function.Call<bool>(Hash.IS_ENTITY_AT_ENTITY, v, carrier, carrier.Model.GetDimensions().X / 2, carrier.Model.GetDimensions().Y, carrier.Model.GetDimensions().Z, true, true, 0) && !v.IsAttached())
                        {
                            Vector3 offset = Function.Call<Vector3>(Hash.GET_OFFSET_FROM_ENTITY_GIVEN_WORLD_COORDS, carrier, v.Position.X, v.Position.Y, v.Position.Z);
                            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY_PHYSICALLY, v, carrier, 0, 0, offset.X, offset.Y, offset.Z, 0, 0, 0, 0, 0f, 0f, 5000f, true, true, true, false, 2); //+ (v.Model.GetDimensions().Y/2f)
                            Finished = true;
                        }
                    }
                }

                if (Finished) return;
                if (carrier.Model.IsCar && carrier.HasBone("attach_female") && carrier.Model != "ramptruck" && carrier.Model != "ramptruck2")
                {
                    //ui.notify("Carrier " + carrier.FriendlyName + " has an 'attach_female' bone, looking for trailers");

                    if (Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER, carrier))
                    {
                        //ui.notify("This carrier has a trailer, getting trailer");
                        Vehicle trailer = null; // GetTrailer(ToCarry);
                        if (trailer == null)
                        {
                            foreach (Vehicle t in World.GetNearbyVehicles(carrier.Position, 30f))
                                if (t.HasBone("attach_male"))
                                {
                                    trailer = t;
                                    break;
                                }
                        }

                        if (trailer != null)
                        {
                            carrier = trailer;
                            //ui.notify("Trailer found, " + carrier.FriendlyName + "(" + carrier.DisplayName + ")");

                            foreach (Vehicle veh in World.GetNearbyVehicles(carrier.Position, 10f))
                                if (veh != OriginalCarrier && veh != carrier)
                                {
                                    ToCarry = veh;
                                    //ui.notify("Found ToCarry, " + ToCarry.FriendlyName);
                                    break;
                                }
                        }
                        else
                        {
                            //ui.notify("Trailer not found, aborting");
                            return;
                        }
                    }
                    else
                    {
                        //ui.notify("This carrier doesn't have trailer, aborting");
                        return;
                    }

                }
                else
                {
                    //ui.notify("Carrier " + carrier.FriendlyName + " does ~o~NOT~w~ have an 'attach_female' bone, must be a normal car");
                    foreach (Vehicle v in World.GetNearbyVehicles(carrier.Position, carrier.Model.GetDimensions().Y * 5))
                    {
                        if (v.IsAttachedTo(carrier))
                        {
                            Detach(carrier, v);

                            //ui.notify("~o~ToCarry already attached, aborting");

                            return;
                        }
                    }

                    if (carrier.Model.IsHelicopter)
                    {
                        foreach (Vehicle veh in World.GetNearbyVehicles(carrier.Position, 30f))
                            if (veh != OriginalCarrier && veh != carrier)
                            {
                                ToCarry = veh;
                                //ui.notify("Found ToCarry, " + ToCarry.FriendlyName);
                                break;
                            }
                    }
                    else
                    {
                        Vector3 back = -(carrier.ForwardVector * carrier.Model.GetDimensions().Y);

                        if (carrier.Model.IsHelicopter) back = -(carrier.UpVector * 30);
                        RaycastResult ray = World.Raycast(carrier.Position, back, 30f, IntersectOptions.Everything, carrier);


                        if (!ray.DitHitEntity) ray = World.Raycast(carrier.Position - carrier.UpVector, back, 30f, IntersectOptions.Everything, carrier);

                        if (ray.DitHitEntity && ray.HitEntity.Model.IsVehicle)
                        {
                            ToCarry = ray.HitEntity as Vehicle;
                            //ui.notify("Carrier: " + carrier.FriendlyName);
                            //ui.notify("ToCarry: " + ToCarry.FriendlyName);

                        }
                        else
                        {
                            if (carrier == Game.Player.Character.CurrentVehicle) UI.Notify("No vehicle found behind yours.");
                            return;
                        }
                    }
                }


            }


            if (!CanWeUse(ToCarry))
            {
                //ui.notify("ToCarry not found, aborting");
                return;
            }

            if (ToCarry.IsAttached())
            {

                return;
            }
            if (carrier == Game.Player.Character.CurrentVehicle) UI.Notify("Attaching " + ToCarry.FriendlyName + " to " + OriginalCarrier.FriendlyName);

            Vector3 CarrierOffset = new Vector3(0, -(carrier.Model.GetDimensions().Y / 2f), 0f);// new Vector3(0, -1.4f, 3f + (ToCarry.Model.GetDimensions().Z * 0.35f));
            Vector3 truckoffset = new Vector3(0f, (ToCarry.Model.GetDimensions().Y / 2f), -ToCarry.HeightAboveGround);
            if (!ToCarry.IsOnAllWheels)
            {
                truckoffset = new Vector3(0f, (ToCarry.Model.GetDimensions().Y / 2f), -(ToCarry.Model.GetDimensions().Z * 0.35f));
            }


            bool Collision = true;

            float pitch = 0f;
            bool NotMadeToCarry = true;
            if (carrier.Model == "mule4" || carrier.Model == "mule5")
            {
                NotMadeToCarry = false;
                CarrierOffset = new Vector3(0, 1.5f, -0.05f);
            }

            /*
            if (carrier.Model == "mule5")
            {
                NotMadeToCarry = false;
                CarrierOffset = new Vector3(0, 1.4f, -0.1f);
            }
            */
            if (carrier.Model == "flatbed")
            {
                NotMadeToCarry = false;
                float farback = 0f;

                CarrierOffset = new Vector3(0, 0.5f + farback, 0.4f); // v.Model.GetDimensions().Z * 0.5f  //
            }

            if (carrier.Model == "barracks4" || carrier.Model == "sturdy2")
            {
                NotMadeToCarry = false;

                CarrierOffset = new Vector3(0, 0.9f, 0.88f); // v.Model.GetDimensions().Z * 0.5f  //
            }
            if (carrier.Model == "ramptruck" || carrier.Model == "ramptruck2")
            {
                NotMadeToCarry = false;
                CarrierOffset = new Vector3(0, -1f, 1f); // v.Model.GetDimensions().Z * 0.5f
                pitch = 5;
            }
            if (carrier.Model == "wastelander")
            {
                NotMadeToCarry = false;
                CarrierOffset = new Vector3(0, 1.5f, 1f); // v.Model.GetDimensions().Z * 0.5f
                                                          //pitch = 5;
            }
            if (carrier.Model == "SKYLIFT")
            {
                NotMadeToCarry = false;
                CarrierOffset = new Vector3(0, -2f, -(ToCarry.Model.GetDimensions().Z / 2) + 0.5f); // v.Model.GetDimensions().Z * 0.5f
                truckoffset = new Vector3(0f, 0f, (ToCarry.Model.GetDimensions().Z * 0.4f));
            }
            //ui.notify("Calculated offsets");
            //ui.notify("Is NOT normal vehicle, attaching");


            if (carrier.Model == "wastelander")
            {
                Collision = false;
                NotMadeToCarry = false;
            }
            if (carrier.Model == "freighttrailer")
            {
                NotMadeToCarry = false;
                CarrierOffset = new Vector3(0, 8f, -1.2f);
            }
            if (carrier.Model == "trflat")
            {
                NotMadeToCarry = false;
                CarrierOffset = new Vector3(0, 3, 0.5f);
            }
            if (carrier.Model == "armytrailer")
            {
                CarrierOffset = new Vector3(0, 0, -1.2f);
                NotMadeToCarry = false;
            }
            if (carrier.Model == "cartrailer" || carrier.Model == "cartrailer2")
            {
                NotMadeToCarry = false;
                CarrierOffset = new Vector3(2.3f, -2.5f, -0.4f);
            }


            if (NotMadeToCarry)
            {
                CarrierOffset = new Vector3(0f, -(carrier.Model.GetDimensions().Y / 2f), 0f);
                truckoffset = new Vector3(0f, (ToCarry.Model.GetDimensions().Y / 2f), 0f);

            }
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY_PHYSICALLY, ToCarry, carrier, 0, 0, CarrierOffset.X, CarrierOffset.Y, CarrierOffset.Z, truckoffset.X, truckoffset.Y, truckoffset.Z, pitch, 0f, 0f, 5000f, !NotMadeToCarry, true, Collision, false, 2); //+ (v.Model.GetDimensions().Y/2f)
            return;

            /* Rope system
         v.Detach();

         Script.Wait(1000);
         Vector3 dynamicoffset = v.GetOffsetFromWorldCoords(carrier.Position);
         //Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY_PHYSICALLY, v, carrier, 0, 0, 0f, 2f, 0f, dynamicoffset.X, dynamicoffset.Y+2f, dynamicoffset.Z, v.Rotation.X, 0f, 0f, 1000f, false, true, true, true, 2); //+ (v.Model.GetDimensions().Y/2f)

         float yoffset = v.Model.GetDimensions().Y/2;
         Rope rope = World.AddRope(RopeType.Normal, v.Position, v.Rotation, v.Position.DistanceTo(carrier.Position), 1f, false);
         rope.ActivatePhysics();
         rope.AttachEntities(v, v.Position+(v.ForwardVector* yoffset), carrier, v.Position + (v.ForwardVector * yoffset) - v.UpVector, (v.Position + (v.ForwardVector * yoffset)).DistanceTo(v.Position + (v.ForwardVector * yoffset) - v.UpVector));
         TrailerRopes.Add(rope);

          yoffset = v.Model.GetDimensions().Y / 2;
          rope = World.AddRope(RopeType.Normal, v.Position, v.Rotation, v.Position.DistanceTo(carrier.Position), 1f, false);
         rope.ActivatePhysics();
         rope.AttachEntities(v, v.Position - (v.ForwardVector * yoffset), carrier, v.Position - (v.ForwardVector * yoffset) - v.UpVector, (v.Position - (v.ForwardVector * yoffset)).DistanceTo(v.Position - (v.ForwardVector * yoffset) - v.UpVector));
         TrailerRopes.Add(rope);
         */
        }
        bool AnyTireBurst(Vehicle v)
        {
            for (int i = -1; i < 10; i++)
            {
                if (Function.Call<bool>(Hash.IS_VEHICLE_TYRE_BURST, v, i, false)) return true;

            }

            return false;
        }
        void OnKeyDown(object sender, KeyEventArgs e)
        {

        }
        void OnKeyUp(object sender, KeyEventArgs e)
        {

        }
        protected override void Dispose(bool dispose)
        {


            base.Dispose(dispose);
        }


        public static bool WasCheatStringJustEntered(string cheat)
        {
            return Function.Call<bool>(Hash._0x557E43C447E700A8, Game.GenerateHash(cheat));
        }


        /// TOOLS ///
        void LoadSettings()
        {
            if (File.Exists(@"scripts\\GTAEGameplay.ini"))
            {

                ScriptSettings config = ScriptSettings.Load(@"scripts\GTAEGameplay.ini");

                CanHook = config.GetValue<bool>("IN_VEHICLE_INTERACTIONS", "CanHook", true);
                RightExit = config.GetValue<bool>("IN_VEHICLE_INTERACTIONS", "RightExit", true);

                SlideHelper = config.GetValue<bool>("GAMEPLAY_IMPROVEMENTS", "SlideHelper", true);
                CanGrab = config.GetValue<bool>("ON_FOOT_INTERACTIONS", "CanGrab", true);
                VehicleInteractionsFoot = config.GetValue<bool>("ON_FOOT_INTERACTIONS", "VehicleInteractions", true);
            }
            else
            {
                WarnPlayer(ScriptName + " " + ScriptVer, "SCRIPT RESET", "~r~" + ScriptVer + " has not found its configuration file. All settings default.");
            }
        }

        void WarnPlayer(string script_name, string title, string message)
        {
            Function.Call(Hash._SET_NOTIFICATION_TEXT_ENTRY, "STRING");
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, message);
            Function.Call(Hash._SET_NOTIFICATION_MESSAGE, "CHAR_SOCIAL_CLUB", "CHAR_SOCIAL_CLUB", true, 0, title, "~b~" + script_name);
        }

        bool CanWeUse(Entity entity)
        {
            return entity != null && entity.Exists();
        }

        void DisplayHelpTextThisFrame(string text)
        {
            Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, "STRING");
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, text);
            Function.Call(Hash._DISPLAY_HELP_TEXT_FROM_STRING_LABEL, 0, false, false, -1);
        }

        public static List<int> Road = new List<int> { 1187676648, 282940568, -108464011, 1187676648, -1084640111 };
        public static List<int> Dirt = new List<int> { 1144315879, 510490462, -1907520769, -1286696947, -1885547121, -700658213, 2128369009,
            -1595148316,-765206029,509508168,1333033863};


        public static int GetGroundHash(Vehicle v)
        {
            Vector3 pos = v.Position + (v.UpVector * 4f); ;
            Vector3 endpos = v.Position + (v.UpVector * -1f);

            int shape = Function.Call<int>(Hash._0x28579D1B8F8AAC80, pos.X, pos.Y, pos.Z, endpos.X, endpos.Y, endpos.Z, 1f, 1, v, 7);

            OutputArgument didhit = new OutputArgument();
            OutputArgument hitpos = new OutputArgument();
            OutputArgument snormal = new OutputArgument();
            OutputArgument materialhash = new OutputArgument();

            OutputArgument entity = new OutputArgument();

            Function.Call(Hash._0x65287525D951F6BE, shape, didhit, hitpos, snormal, materialhash, entity);

            if (didhit.GetResult<bool>() == true)
            {
                //UI.ShowSubtitle(materialhash.GetResult<int>().ToString());
            }
            else
            {
                //UI.ShowSubtitle("Didn't hit anything");
            }

            return materialhash.GetResult<int>();
        }
        //Queued help text


        public static List<String> MessageQueue = new List<String>();
        public static int MessageQueueInterval = 8000;
        public static int MessageQueueReferenceTime = 0;
        public static bool CanDisplay = false;
        public static void HandleMessages()
        {

            if (!CanDisplay && !Function.Call<bool>(Hash.IS_HELP_MESSAGE_BEING_DISPLAYED)) CanDisplay = true;
            if (MessageQueue.Count > 0 && CanDisplay)
            {

                Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, "STRING");
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, MessageQueue[0]);
                Function.Call(Hash._DISPLAY_HELP_TEXT_FROM_STRING_LABEL, 0, false, false, -1);
            }
            else
            {
                MessageQueueReferenceTime = Game.GameTime;
            }
            if (Game.GameTime > MessageQueueReferenceTime + MessageQueueInterval)
            {
                if (MessageQueue.Count > 0)
                {
                    MessageQueue.RemoveAt(0);
                    CanDisplay = false;
                }
                MessageQueueReferenceTime = Game.GameTime;
            }
        }
        public static void AddQueuedHelpText(string text)
        {
            if (!MessageQueue.Contains(text)) MessageQueue.Add(text);
        }

        public static void ClearAllHelpText(string text)
        {
            MessageQueue.Clear();
        }


        public static List<String> NotificationQueueText = new List<String>();
        public static List<String> NotificationQueueAvatar = new List<String>();
        public static List<String> NotificationQueueAuthor = new List<String>();
        public static List<String> NotificationQueueTitle = new List<String>();

        public static int NotificationQueueInterval = 8000;
        public static int NotificationQueueReferenceTime = 0;
        public static void HandleNotifications()
        {
            if (Game.GameTime > NotificationQueueReferenceTime)
            {

                if (NotificationQueueAvatar.Count > 0 && NotificationQueueText.Count > 0 && NotificationQueueAuthor.Count > 0 && NotificationQueueTitle.Count > 0)
                {
                    NotificationQueueReferenceTime = Game.GameTime + ((NotificationQueueText[0].Length / 10) * 1000);
                    Notify(NotificationQueueAvatar[0], NotificationQueueAuthor[0], NotificationQueueTitle[0], NotificationQueueText[0]);
                    NotificationQueueText.RemoveAt(0);
                    NotificationQueueAvatar.RemoveAt(0);
                    NotificationQueueAuthor.RemoveAt(0);
                    NotificationQueueTitle.RemoveAt(0);
                }
            }
        }

        public static void AddNotification(string avatar, string author, string title, string text)
        {
            NotificationQueueText.Add(text);
            NotificationQueueAvatar.Add(avatar);
            NotificationQueueAuthor.Add(author);
            NotificationQueueTitle.Add(title);
        }
        public static void CleanNotifications()
        {
            NotificationQueueText.Clear();
            NotificationQueueAvatar.Clear();
            NotificationQueueAuthor.Clear();
            NotificationQueueTitle.Clear();
            NotificationQueueReferenceTime = Game.GameTime;
            Function.Call(Hash._REMOVE_NOTIFICATION, CurrentNotification);
        }

        public static int CurrentNotification;
        public static void Notify(string avatar, string author, string title, string message)
        {
            if (avatar != "" && author != "" && title != "")
            {
                Function.Call(Hash._SET_NOTIFICATION_TEXT_ENTRY, "STRING");
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, message);
                CurrentNotification = Function.Call<int>(Hash._SET_NOTIFICATION_MESSAGE, avatar, avatar, true, 0, title, author);
            }
            else
            {
                UI.Notify(message);
            }
        }

    }
}