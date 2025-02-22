﻿using ChatClient.MVVM.Core;
using System.Windows;
using ChatClient.MVVM.ViewModel;
using ChatClient.MVVM.View;


namespace ChatClient.MVVM.ViewModel
{
    class LoginViewModel : ObservableObject
    {
        public string ProfileImage { get; set; }
        public string Username { get; set; }
        public string Password { get; set; } = "password";
        public RelayCommand LoginCommand { get; set; }

        private readonly List<string> _usernames = new()
        {
            "xXGamer360NoScopeXx",
            "CoolCat123",
            "N00b_Destroyer_1337",
            "I_H4t3_L1f3_xD",
            "xX_Slayer_Xx",
            "SkullzOnF1r3",
            "Xx_D3athStalker_xX",
            "Pwnz0r",
            "xXx_1337_xXx",
            "R4venBlade",
            "D3athM4ch1n3",
            "AKSprayLord",
            "Pray_N_Spray",
        };

        public LoginViewModel()
        {
            System.Diagnostics.Debug.WriteLine("LoginViewModel called");

            var random = new Random();
            Username = _usernames[random.Next(_usernames.Count)];
            System.Diagnostics.Debug.WriteLine($"default username: {Username}");

            LoginCommand = new RelayCommand(o =>
            {
                System.Diagnostics.Debug.WriteLine($"Attempting login as {Username}"); //fFIX THIS -CAUSES 2 WINDOWS TO OPEN
                var mainWindow = new MainWindow()
                {
                    DataContext = new MainViewModel(Username)
                };
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();

                foreach (Window window in Application.Current.Windows)
                {
                    if (window is LoginView)
                    {
                        window.Close();
                        break;
                    }
                }
            });
        }
    }

}