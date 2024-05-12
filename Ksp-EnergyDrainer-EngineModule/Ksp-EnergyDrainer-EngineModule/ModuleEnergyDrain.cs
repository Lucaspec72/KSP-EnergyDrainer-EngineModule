﻿using System;
using UnityEngine;

namespace Ksp_EnergyDrainer_EngineModule
{
    public class ModuleEnergyDrain : PartModule
    {
        //Guiname and GuiActive are tests to see if they show up.

        //CFG Variables
        [KSPField(guiName = "Target Resource", guiActive = false, isPersistant = false)]
        public string resourceDrained;
        [KSPField(guiName = "Base Consumption", guiActive = false, isPersistant = false)]
        public float consumption;

        //Display Variables - Are ment to be visible in normal gameplay
        [KSPField(guiName = "Status", guiActive = true, isPersistant = false)]
        public string Status;

        //Other Variables

        //Hook to ModuleEnginesFX
        [KSPField]
        public ModuleEnginesFX EngineModule;
        //Resource Hash
        [KSPField]
        public int resHash = 0;
        //Boolean flag to say if something is wrong.
        [KSPField(guiName = "Hook Error", guiActive = false)]
        public bool hookError = true;
        [KSPField(guiName = "Resource Error", guiActive = false)]
        public bool resourceError = true;
        //Manual Start button
        [KSPEvent(name = "ManualIgnition", guiName = "Manual Ignition", active = true, guiActive = true)]
        public void ManualIgnition()
        {
            InitialiseModule();
            Events["ManualIgnition"].guiActive = false;
            this.part.force_activate();
            this.EngineModule.PlayEngageFX();
        }



        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            {
                if (state != StartState.Editor && state != StartState.None)
                {
                    //things here won't get run either way, just keeping around to know it doesn't work.
                    Debug.Log("[ModuleEnergyDrain]:" + "ONSTART TRIGGER");
                    //InitialiseModule();
                    //this fixes it, but wrecks staging, since engine is technically started.
                    //Need to find some way to make the OnFixedUpdate of the module trigger.
                    //this.part.force_activate();
                    //EngineModule.Shutdown();
                }
                else
                {

                }
            }
        }

        public void InitialiseModule()
        {
            //A HORRIBLE MESS, but it works
            try
            {
                //Get the hook to the ModuleEnginesFX, and the hash to the ressource drained. This worked earlier : this.part.FindModuleImplementing<ModuleEnginesFX>();
                EngineModule = this.part.FindModuleImplementing<ModuleEnginesFX>();
                resHash = PartResourceLibrary.Instance.GetDefinition(resourceDrained).id;
                Debug.Log("[ModuleEnergyDrain]:" + "resHash");
                Debug.Log("[ModuleEnergyDrain]:" + resHash);
                Debug.Log("[ModuleEnergyDrain]:" + "EngineModule");
                Debug.Log("[ModuleEnergyDrain]:" + EngineModule.currentThrottle);
            }
            catch (Exception e) 
            {
                Debug.Log("[ModuleEnergyDrain]:" + e.ToString());
            }
            //Had problems with catch, so i run tests here.
            if (EngineModule != null)
            {
                //Base Errors are here to see if things run. It should be updated as soon as the next update. if it doesn't, there's a problem somewhere.
                hookError = false;
                Debug.Log("[ModuleEnergyDrain]: Base Hook Log (no-error)");
            }
            else
            {
                hookError = true;
                Debug.Log("[ModuleEnergyDrain]: Base Hook Log (ERROR)");
            }
            if (resHash != 0)
            {
                resourceError = false;
                Debug.Log("[ModuleEnergyDrain]: Base Resource Log (no-error)");
            }
            else
            {
                resourceError = true;
                Debug.Log("[ModuleEnergyDrain]: Base Resource Log (ERROR)");
            }
            Status = "Not running OnFixedUpdate Error";
        }

        public override void OnActive()
        {
            base.OnActive();
            {
                //doesn't seem to run, or at least not how i want it to.
            }
        }
        public override void OnInactive()
        {
            base.OnInactive();
            {

            }
        }

        public override void OnFixedUpdate()
        {
            if (hookError == true || resourceError == true)
            {
                Debug.Log("[ModuleEnergyDrain]: Error Detected, re-Initialising Module...");
                InitialiseModule();
            }
            if (EngineModule.isEnabled)
            {
                //get the consumption by using the base consumption, the amount of time since last update, and the current throttle.
                double consumptionDelta = consumption * TimeWarp.fixedDeltaTime * EngineModule.currentThrottle;
                //drain the ressource
                double resReturn = this.vessel.RequestResource(this.vessel.Parts[0], resHash, consumptionDelta, false);
                //check if it managed to drain what was needed.
                if (Math.Abs(consumptionDelta - resReturn) < 0.01)
                {
                    //Optionally do some other stuff while engine is running.
                    Status = "Running";
                }
                else
                {
                    EngineModule.Shutdown(); //this is a placeholder function call, idk if Shutdown does what i think it does.
                }
            }
            else
            {
                //check if there was a error loading
                if (!hookError && !resourceError)
                {
                    //check if there's enough of the ressource to last one second. Not tested, but math should be sound
                    if (this.vessel.RequestResource(this.vessel.Parts[0], resHash, consumption, true) - consumption < 0.01)
                    {
                        Status = "Idle";
                    }
                    else
                    {
                        Status = "Out of " + resourceDrained;
                    }
                }
                else
                {
                    if (hookError == true && resourceError == true)
                    {
                        Status = "Critical Error, contact mod maker";
                    }
                    else if (hookError == true)
                    {
                        Status = "Error, Module Can't hook to Engine";
                    }
                    else //assumes it's ressource error, wouldn't get here otherwise
                    {
                        Status = "Error, Resource in part CFG unknown";
                    }
                }

            }
        }

    }
}