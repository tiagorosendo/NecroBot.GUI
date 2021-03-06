﻿using Microsoft.CSharp.RuntimeBinder;
using PoGo.NecroBot.Logic.Common;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.Logging;
using PoGo.NecroBot.Logic.State;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Responses;
using System;
using System.Globalization;

namespace PoGo.NecroBot.GUI
{
    public class EventListener
    {
        private PokeGUI gui;
        public EventListener(PokeGUI gui)
        {
            this.gui = gui;
        }
        public void HandleEvent(ProfileEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventProfileLogin,
                evt.Profile.PlayerData.Username ?? ""));
        }

        public void HandleEvent(ErrorEvent evt, ISession session)
        {
            Logger.Write(evt.ToString(), LogLevel.Error);
        }

        public void HandleEvent(NoticeEvent evt, ISession session)
        {
            Logger.Write(evt.ToString());
        }

        public void HandleEvent(WarnEvent evt, ISession session)
        {
            Logger.Write(evt.ToString(), LogLevel.Warning);

            if (evt.RequireInput)
            {
                Logger.Write(session.Translation.GetTranslation(TranslationString.RequireInputText));
                Console.ReadKey();
            }

        }

        public void HandleEvent(UseLuckyEggEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventUsedLuckyEgg, evt.Count), LogLevel.Egg);
        }

        public void HandleEvent(PokemonEvolveEvent evt, ISession session)
        {
            Logger.Write(evt.Result == EvolvePokemonResponse.Types.Result.Success
                ? session.Translation.GetTranslation(TranslationString.EventPokemonEvolvedSuccess, evt.Id, evt.Exp)
                : session.Translation.GetTranslation(TranslationString.EventPokemonEvolvedFailed, evt.Id, evt.Result,
                    evt.Id),
                LogLevel.Evolve);
        }

        public void HandleEvent(TransferPokemonEvent evt, ISession session)
        {
            Logger.Write(
                session.Translation.GetTranslation(TranslationString.EventPokemonTransferred, evt.Id.ToString().PadRight(20), evt.Cp,
                    evt.Perfection.ToString("0.00"), evt.BestCp, evt.BestPerfection.ToString("0.00"), evt.FamilyCandies),
                LogLevel.Transfer);
        }

        public void HandleEvent(ItemRecycledEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventItemRecycled, evt.Count, evt.Id),
                LogLevel.Recycling);
        }

        public void HandleEvent(EggIncubatorStatusEvent evt, ISession session)
        {
            this.gui.updateIncubator(evt.IncubatorId, evt.KmWalked.ToString("F3"));
            Logger.Write(evt.WasAddedNow
                ? session.Translation.GetTranslation(TranslationString.IncubatorPuttingEgg, evt.KmRemaining)
                : session.Translation.GetTranslation(TranslationString.IncubatorStatusUpdate, evt.KmRemaining), LogLevel.Egg);
        }

        public void HandleEvent(EggHatchedEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.IncubatorEggHatched, evt.PokemonId.ToString()), LogLevel.Egg);
        }

        public void HandleEvent(FortUsedEvent fortUsedEvent, ISession session)
        {
            var itemString = fortUsedEvent.InventoryFull
            ? session.Translation.GetTranslation(TranslationString.InvFullPokestopLooting)
            : fortUsedEvent.Items;
            Logger.Write(String.Format("Name: {0} \n - XP:    {1} \n - Gems:  {2} \n - Items: {3}", fortUsedEvent.Name, fortUsedEvent.Exp, fortUsedEvent.Gems, itemString), LogLevel.Pokestop);

            this.gui.addPokestopVisited(new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),fortUsedEvent.Name, fortUsedEvent.Exp.ToString(), fortUsedEvent.Gems.ToString(), itemString, fortUsedEvent.Latitude.ToString("0.00000"), fortUsedEvent.Longitude.ToString("0.00000") });
        }

        public void HandleEvent(FortFailedEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventFortFailed, evt.Name, evt.Try, evt.Max),
                LogLevel.Pokestop, ConsoleColor.DarkRed);
        }

        public void HandleEvent(FortTargetEvent fortTargetEvent, ISession session)
        {
            int intTimeForArrival = (int)(fortTargetEvent.Distance / (session.LogicSettings.WalkingSpeedInKilometerPerHour * 0.2777));
            this.gui.UIThread(() => this.gui.labelNext.TextLine1 = $"Next stop: {fortTargetEvent.Name} - {Math.Round(fortTargetEvent.Distance)}m - {intTimeForArrival} seconds");
            Logger.Write($"Next stop: {fortTargetEvent.Name} - {Math.Round(fortTargetEvent.Distance)}m - {intTimeForArrival} seconds", LogLevel.Pokestop);
        }

        private void HandleEvent(PokemonCaptureEvent pokemonCaptureEvent, ISession session)
        {
            Func<ItemId, string> returnRealBallName = a =>
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (a)
                {
                    case ItemId.ItemPokeBall:
                        return session.Translation.GetTranslation(TranslationString.Pokeball);
                    case ItemId.ItemGreatBall:
                        return session.Translation.GetTranslation(TranslationString.GreatPokeball);
                    case ItemId.ItemUltraBall:
                        return session.Translation.GetTranslation(TranslationString.UltraPokeball);
                    case ItemId.ItemMasterBall:
                        return session.Translation.GetTranslation(TranslationString.MasterPokeball);
                    default:
                        return session.Translation.GetTranslation(TranslationString.CommonWordUnknown);
                }
            };

            var catchType = pokemonCaptureEvent.CatchType;

            string strStatus;
            switch (pokemonCaptureEvent.Status)
            {
                case CatchPokemonResponse.Types.CatchStatus.CatchError:
                    strStatus = session.Translation.GetTranslation(TranslationString.CatchStatusError);
                    break;
                case CatchPokemonResponse.Types.CatchStatus.CatchEscape:
                    strStatus = session.Translation.GetTranslation(TranslationString.CatchStatusEscape);
                    break;
                case CatchPokemonResponse.Types.CatchStatus.CatchFlee:
                    strStatus = session.Translation.GetTranslation(TranslationString.CatchStatusFlee);
                    break;
                case CatchPokemonResponse.Types.CatchStatus.CatchMissed:
                    strStatus = session.Translation.GetTranslation(TranslationString.CatchStatusMissed);
                    break;
                case CatchPokemonResponse.Types.CatchStatus.CatchSuccess:
                    strStatus = session.Translation.GetTranslation(TranslationString.CatchStatusSuccess);
                    break;
                default:
                    strStatus = pokemonCaptureEvent.Status.ToString();
                    break;
            }

            var catchStatus = pokemonCaptureEvent.Attempt > 1
                ? session.Translation.GetTranslation(TranslationString.CatchStatusAttempt, strStatus, pokemonCaptureEvent.Attempt)
                : session.Translation.GetTranslation(TranslationString.CatchStatus, strStatus);

            var familyCandies = pokemonCaptureEvent.FamilyCandies > 0
                ? pokemonCaptureEvent.FamilyCandies.ToString()
                : "";

            string message;

            if (pokemonCaptureEvent.Status == CatchPokemonResponse.Types.CatchStatus.CatchSuccess)
            {
                message = String.Format("({0}) | ({1}) {2} \n - Lvl: {3}\n - CP: ({4}/{5})\n - IV: {6}%\n - Chance: {7}%\n - from {8}m dist with a {9} ({10} left).\n - {11} EXP earned\n - {12}\n - lat: {13}, long: {14}", catchStatus, catchType, session.Translation.GetPokemonTranslation(pokemonCaptureEvent.Id),
                pokemonCaptureEvent.Level, pokemonCaptureEvent.Cp, pokemonCaptureEvent.MaxCp, pokemonCaptureEvent.Perfection.ToString("0.00"), pokemonCaptureEvent.Probability,
                pokemonCaptureEvent.Distance.ToString("F2"),
                returnRealBallName(pokemonCaptureEvent.Pokeball), pokemonCaptureEvent.BallAmount,
                pokemonCaptureEvent.Exp, familyCandies, pokemonCaptureEvent.Latitude.ToString("0.000000"), pokemonCaptureEvent.Longitude.ToString("0.000000"));
                Logger.Write(message, LogLevel.Caught);

                this.gui.addPokemonCaught(new string[] {
                    $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}",
                    catchStatus,
                    session.Translation.GetPokemonTranslation(pokemonCaptureEvent.Id),
                    pokemonCaptureEvent.Level.ToString(),
                    $"{pokemonCaptureEvent.Cp} / {pokemonCaptureEvent.MaxCp}",
                    pokemonCaptureEvent.Perfection.ToString("0.00") + "%",
                    pokemonCaptureEvent.Probability.ToString("0.00") + "%",
                    pokemonCaptureEvent.Distance.ToString("F2") + "m",
                    catchType,
                    $"{returnRealBallName(pokemonCaptureEvent.Pokeball)} ({pokemonCaptureEvent.BallAmount} left)",
                    pokemonCaptureEvent.Exp.ToString() + "xp",
                    familyCandies});
                this.gui.sendEvent("Catch", "success", pokemonCaptureEvent.Id.ToString(), pokemonCaptureEvent.Cp);
            }
            else
            {
                message = session.Translation.GetTranslation(TranslationString.EventPokemonCaptureFailed, catchStatus, catchType, session.Translation.GetPokemonTranslation(pokemonCaptureEvent.Id),
                pokemonCaptureEvent.Level, pokemonCaptureEvent.Cp, pokemonCaptureEvent.MaxCp, pokemonCaptureEvent.Perfection.ToString("0.00"), pokemonCaptureEvent.Probability,
                pokemonCaptureEvent.Distance.ToString("F2"),
                returnRealBallName(pokemonCaptureEvent.Pokeball), pokemonCaptureEvent.BallAmount,
                pokemonCaptureEvent.Latitude.ToString("0.000000"), pokemonCaptureEvent.Longitude.ToString("0.000000"));
                Logger.Write(message, LogLevel.Flee);
            }

        }
        private void HandleEvent(NoPokeballEvent noPokeballEvent, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventNoPokeballs, noPokeballEvent.Id, noPokeballEvent.Cp),
                LogLevel.Caught);
        }

        private void HandleEvent(UseBerryEvent useBerryEvent, ISession session)
        {
            string strBerry;
            switch (useBerryEvent.BerryType)
            {
                case ItemId.ItemRazzBerry:
                    strBerry = session.Translation.GetTranslation(TranslationString.ItemRazzBerry);
                    break;
                default:
                    strBerry = useBerryEvent.BerryType.ToString();
                    break;
            }

            Logger.Write(session.Translation.GetTranslation(TranslationString.EventUseBerry, strBerry, useBerryEvent.Count),
                LogLevel.Berry);
        }

        private void HandleEvent(SnipeEvent snipeEvent, ISession session)
        {
            Logger.Write(snipeEvent.ToString(), LogLevel.Sniper);
        }

        private void HandleEvent(SnipeScanEvent snipeScanEvent, ISession session)
        {
            Logger.Write(snipeScanEvent.PokemonId == PokemonId.Missingno
                ? ((snipeScanEvent.Source != null) ? "(" + snipeScanEvent.Source + ") " : null) + session.Translation.GetTranslation(TranslationString.SnipeScan,
                    $"{snipeScanEvent.Bounds.Latitude},{snipeScanEvent.Bounds.Longitude}")
                : ((snipeScanEvent.Source != null) ? "(" + snipeScanEvent.Source + ") " : null) + session.Translation.GetTranslation(TranslationString.SnipeScanEx, session.Translation.GetPokemonTranslation(snipeScanEvent.PokemonId),
                    snipeScanEvent.Iv > 0 ? snipeScanEvent.Iv.ToString(CultureInfo.InvariantCulture) : session.Translation.GetTranslation(TranslationString.CommonWordUnknown),
                    $"{snipeScanEvent.Bounds.Latitude},{snipeScanEvent.Bounds.Longitude}"), LogLevel.Sniper);

            this.gui.setSniper(snipeScanEvent.Bounds.Latitude.ToString(), snipeScanEvent.Bounds.Longitude.ToString());
        }

        private void HandleEvent(EvolveCountEvent evolveCountEvent, ISession session)
        {
            this.gui.UIThread(() => this.gui.labelEvolvable.TextLine2 = evolveCountEvent.Evolves.ToString());
        }

        private void HandleEvent(UpdateEvent updateEvent, ISession session)
        {
            Logger.Write(updateEvent.ToString(), LogLevel.Update);
        }

        private void HandleEvent(SnipeModeEvent snipeModeEvent, ISession session)
        {
            string onOff = snipeModeEvent.Active ? "On" : "Off";
            Logger.Write($"Sniper {onOff}", LogLevel.Sniper);
        }
        /*private void HandleEvent(DisplayHighestsPokemonEvent displayEvent, ISession session)
        {
            
        }*/
        private void HandleEvent(UpdatePositionEvent posEvent, ISession session)
      {
            Logger.Write($"Position changed. lat={posEvent.Latitude.ToString("0.00000")}, lng={posEvent.Longitude.ToString("0.00000")}", LogLevel.Debug);
      }

        private void HandleEvent(PokeStopListEvent polEvent, ISession session)
        {
            Logger.Write($"PokeStopList has {polEvent.Forts.Count} items", LogLevel.Debug);
        }

        private void HandleEvent(HumanWalkingEvent humanWalkingEvent, ISession session)
        {
            Logger.Write($"Moving like a human in speed {humanWalkingEvent.CurrentWalkingSpeed} km/h | Old Walking Speed {humanWalkingEvent.OldWalkingSpeed}", LogLevel.Debug);
        }

        public void Listen(IEvent evt, ISession session)
        {
            dynamic eve = evt;

            try
            {
                HandleEvent(eve, session);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (RuntimeBinderException) {
                Logger.Write($"No listener for event {evt.GetType().Name}", LogLevel.None);
            }
            catch (Exception ex)
            {
                try
                {
                    Logger.Write($"Unhandeled exception ({ex.GetType().Name}) on event {evt.GetType().Name}: {ex.Message}", LogLevel.Error);
                }
                catch
                {
                    //die here..
                }
            }
        }
    }
}
