using DiscordRPC;
using ModLoader;
using ModLoader.Helpers;
using SFS.Builds;
using SFS.IO;
using SFS.World;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

using Tracker = SFS.Stats.StatsRecorder.Tracker;

namespace RichPresenceSFS
{
    public class Main : Mod
    {
        public override string ModNameID => "richpresence";

        public override string DisplayName => "Rich Presence";

        public override string Author => "VerdiX";

        public override string MinimumGameVersionNecessary => "1.5.10.2";

        public override string ModVersion => "1.0";

        public override string Description => "Provides unofficial Rich Presence support for SFS.";

        public FilePath rpc;

        public override void Early_Load()
        {
            Assembly.Load(ResourceFile.DiscordRPC);
            new GameObject("RPC").AddComponent<RPC>();
        }
    }

    public class RPC : MonoBehaviour
    {
        DiscordRpcClient client;
        string sceneName;
        private void Start()
        {
            client = new DiscordRpcClient("1324636539543617556");
            client.Initialize();

            Application.quitting += () =>
            {
                client.Dispose();
            };

            SceneManager.sceneLoaded += (Scene s, LoadSceneMode lsm) =>
            {
                sceneName = s.name;
            };
            InvokeRepeating(nameof(UpdateState), 1f, 1f);
        }

        private void UpdateState()
        {
            string GetPlanetName(Rocket r)
            {
                return r.location.planet.Value.codeName;
            }
            string GetDetails()
            {
                if (sceneName == "Build_PC")
                {
                    return "Part count: " + BuildManager.main.buildGrid.activeGrid.partsHolder.parts.Count;
                }
                if (sceneName == "World_PC")
                {
                    if (PlayerController.main.player.Value is Rocket rocket)
                    {
                        var orbit = Orbit.TryCreateOrbit(rocket.location.Value, true, false, out bool success);

                        if (success && !rocket.stats.tracker.state_Landed)
                        {
                            if (rocket.stats.tracker.state_Orbit == Tracker.State_Orbit.Sub)
                            {
                                return "Alt: " + (rocket.location.Value.Height / 1000).Round(1) + " km, Ap: " + (Math.Max(0, orbit.apoapsis - orbit.Planet.Radius) / 1000).Round(1) + " km" + (orbit.periapsis > orbit.Planet.Radius ? (", Pe: " + (Math.Max(0, orbit.periapsis - orbit.Planet.Radius) / 1000).Round(1) + " km") : "");
                            }
                            else if (rocket.stats.tracker.state_Orbit == Tracker.State_Orbit.Esc)
                            {
                                return "Alt: " + (rocket.location.Value.Height / 1000).Round(1) + " km, Pe: " + (Math.Max(0, orbit.apoapsis + orbit.Planet.Radius) / 1000).Round(1) + " km";
                            }
                            else if (rocket.stats.tracker.state_Orbit == Tracker.State_Orbit.None)
                            {
                                var landmark = rocket.location.planet.Value.landmarks.ToList().GetBest((a, b) =>
                                {
                                    return Math.Abs(rocket.location.Value.position.AngleDegrees - a.position.AngleDegrees) < Math.Abs(rocket.location.Value.position.AngleDegrees - b.position.AngleDegrees);
                                });
                                return landmark != null ? ("Near" + landmark.displayName) : "Location: Unknown";
                            }
                            return "Alt: " + (rocket.location.Value.Height / 1000).Round(1) + " km, Ap: " + (Math.Max(0, orbit.apoapsis - orbit.Planet.Radius) / 1000).Round(1) + " km, Pe: " + (Math.Max(0, orbit.periapsis - orbit.Planet.Radius) / 1000).Round(1) + " km";
                        }

                        if (rocket.stats.tracker.state_Landed)
                        {
                            var landmark = rocket.location.planet.Value.landmarks.ToList().GetBest((a, b) =>
                            {
                                return Math.Abs(rocket.location.Value.position.AngleDegrees - a.position.AngleDegrees) < Math.Abs(rocket.location.Value.position.AngleDegrees - b.position.AngleDegrees);
                            });
                            return landmark != null ? ("Near" + landmark.displayName) : "Location: Unknown";
                        }
                    }
                }

                return "";
            }
            string GetState()
            {
                if (sceneName == "Build_PC")
                {
                    return "Building a rocket";
                }
                if (sceneName == "World_PC")
                {
                    if (PlayerController.main.player.Value is Rocket rocket)
                    {
                        var orbit = Orbit.TryCreateOrbit(rocket.location.Value, true, false, out bool success);
                        if (rocket.stats.tracker.state_Landed)
                        {
                            return "Landed on " + GetPlanetName(rocket);
                        }

                        switch (rocket.stats.tracker.state_Orbit)
                        {
                            case Tracker.State_Orbit.None:
                                return "Nearby " + GetPlanetName(rocket);
                            case Tracker.State_Orbit.Sub:
                                return "Suborbital near " + GetPlanetName(rocket);
                            case Tracker.State_Orbit.Low:
                                return "In Low " + GetPlanetName(rocket) + " Orbit";
                            case Tracker.State_Orbit.High:
                                return "In High " + GetPlanetName(rocket) + " Orbit";
                            case Tracker.State_Orbit.Trans:
                                return "In " + GetPlanetName(rocket) + " Transfer Orbit";
                            case Tracker.State_Orbit.Esc:
                                return "Escaping " + GetPlanetName(rocket);
                        }
                    }
                    else
                    {
                        return "In World";
                    }
                }

                return "In menu";
            }

            client.SetPresence(new RichPresence()
            {
                Details = GetDetails(),
                State = GetState(),
                Assets = new DiscordRPC.Assets()
                {
                    LargeImageKey = "game-logo",
                    LargeImageText = "Spaceflight Simulator"
                }
            });
        }
    }
}
