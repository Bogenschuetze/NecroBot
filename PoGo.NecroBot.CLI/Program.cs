﻿#region using directives

using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using PoGo.NecroBot.Logic;
using PoGo.NecroBot.Logic.Logging;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Utils;

#endregion

namespace PoGo.NecroBot.CLI
{
    internal class Program
    {
        public static void LoginWithGoogle(string usercode, string uri)
        {
            try
            {
                Logger.Write("Google Device Code copied to clipboard");
                Thread.Sleep(2000);
                Process.Start(uri);
                var thread = new Thread(() => Clipboard.SetText(usercode)); //Copy device code
                thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
                thread.Start();
                thread.Join();
            }
            catch (Exception)
            {
                Logger.Write("Couldnt copy to clipboard, do it manually", LogLevel.Error);
                Logger.Write($"Goto: {uri} & enter {usercode}", LogLevel.Error);
            }
        }

        private static void Main()
        {
            Logger.SetLogger(new ConsoleLogger(LogLevel.Info));

            var machine = new StateMachine();
            var stats = new Statistics();
            stats.DirtyEvent += () => Console.Title = stats.ToString();

            var aggregator = new StatisticsAggregator(stats);
            var listener = new ConsoleEventListener();

            machine.EventListener += listener.Listen;
            machine.EventListener += aggregator.Listen;

            machine.SetFailureState(new LoginState());

            GlobalSettings settings = GlobalSettings.Load("\\config\\config.json");

            var context = new Context(new ClientSettings(settings), new LogicSettings(settings));
            context.Client.Login.GoogleDeviceCodeEvent += LoginWithGoogle;

            machine.AsyncStart(new VersionCheckState(), context);

            Console.ReadLine();
        }
    }
}