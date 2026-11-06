using System;
using System.Collections.Generic;
using Duzhe.Chapters;
using Duzhe.River;
using UnityEngine;
using UnityEditor;
namespace Duzhe.EditorTools
{
    public static class RiverMotionVerification
    {
        private static void Require(bool condition, string message)
        { if (!condition) throw new Exception(message); }
        public static void RunAndBuild()
        {
            GameObject boatObject = new GameObject("RiverMotionProbe");
            GameObject hazards = new GameObject("RiverHazardProbe");
            try
            {
                RiverSettings settings = new RiverSettings();
                BoatController boat = boatObject.AddComponent<BoatController>();
                boat.Bind(settings);
                float initialY = boat.BoatY;
                boat.Advance(settings.sailDuration * 0.5f);
                Require(Mathf.Abs(boat.BoatY - initialY - BoatController.VoyageDistance * 0.5f) < 0.001f, "Boat did not advance in world space.");
                float pausedY = boat.BoatY;
                boat.Advance(0f);
                Require(boat.BoatY == pausedY, "Paused boat moved.");
                boat.Steer(1f,100f);
                Require(Mathf.Abs(boat.BoatX - settings.steerRangeX) < 0.001f, "Steering escaped the river bounds.");
                boat.ResetPosition();
                Require(boat.BoatY == initialY && boat.BoatX == 0f, "Retry did not reset the full voyage position.");
                WhirlpoolSpawner spawner = hazards.AddComponent<WhirlpoolSpawner>();
                spawner.Bind(new List<RiverWave> { new RiverWave { arrivalTime=1f, bannedLanes=new[]{0}, whirlXs=new[]{-3f} } }, settings, initialY);
                spawner.SpawnDue(new RiverSession());
                Require(hazards.transform.childCount == 1, "Hazard wave was not spawned.");
                Transform whirl = hazards.transform.GetChild(0);
                float before = whirl.position.y - initialY;
                spawner.FollowBoat(initialY + 2f);
                Require(Mathf.Abs(whirl.position.y - (initialY+2f) - before)<0.001f,"Forward travel changed hazard arrival distance.");
                spawner.MoveAll(0.1f,settings);
                Require(Mathf.Abs(whirl.position.y-(initialY+2f)-before+settings.forwardSpeed*0.1f)<0.001f,"Hazard approach speed changed.");
                Debug.Log("DZ_RIVER_MOTION=PASS: forward travel, pause, steering, retry, hazard timing");
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
                return;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(boatObject);
                UnityEngine.Object.DestroyImmediate(hazards);
            }
            DuzheBuildTools.BuildWindowsFromCommandLine();
        }
    }
}
