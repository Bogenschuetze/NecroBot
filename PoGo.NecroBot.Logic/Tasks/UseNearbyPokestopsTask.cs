﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Utils;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Map.Fort;
namespace PoGo.NecroBot.Logic.Tasks
{
    class UseNearbyPokestopsTask
    {
        //Please do not change GetPokeStops() in this file, it's specifically set
        //to only find stops within 40 meters
        //this is for gpx pathing, we are not going to the pokestops,
        //so do not make it more than 40 because it will never get close to those stops.
        public static void Execute(Context ctx, StateMachine machine)
        {
            var pokestopList = GetPokeStops(ctx);

            while (pokestopList.Any())
            {
                pokestopList =
                    pokestopList.OrderBy(
                        i =>
                            LocationUtils.CalculateDistanceInMeters(ctx.Client.CurrentLatitude,
                                ctx.Client.CurrentLongitude, i.Latitude, i.Longitude)).ToList();
                var pokeStop = pokestopList[0];
                pokestopList.RemoveAt(0);

                ctx.Client.Fort.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude).Wait();

                var fortSearch =
                    ctx.Client.Fort.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude).Result;

                if (fortSearch.ExperienceAwarded > 0)
                {
                    machine.Fire(new FortUsedEvent
                    {
                        Exp = fortSearch.ExperienceAwarded,
                        Gems = fortSearch.GemsAwarded,
                        Items = StringUtils.GetSummedFriendlyNameOfItemAwardList(fortSearch.ItemsAwarded)
                    });
                }

                Thread.Sleep(1000);

                RecycleItemsTask.Execute(ctx, machine);

                if (ctx.LogicSettings.TransferDuplicatePokemon)
                {
                    TransferDuplicatePokemonTask.Execute(ctx, machine);
                }
            }
        }


        private static List<FortData> GetPokeStops(Context ctx)
        {
            var mapObjects = ctx.Client.Map.GetMapObjects().Result;

            // Wasn't sure how to make this pretty. Edit as needed.
            var pokeStops = mapObjects.MapCells.SelectMany(i => i.Forts)
                .Where(
                    i =>
                        i.Type == FortType.Checkpoint &&
                        i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime() &&
                        ( // Make sure PokeStop is within 40 meters or else it is pointless to hit it
                            LocationUtils.CalculateDistanceInMeters(
                                ctx.Settings.DefaultLatitude, ctx.Settings.DefaultLongitude,
                                i.Latitude, i.Longitude) < 40) ||
                        ctx.LogicSettings.MaxTravelDistanceInMeters == 0
                );

            return pokeStops.ToList();
        }
    }


}
