﻿// (c) 2020 Manabu Tonosaki
// Licensed under the MIT license.

using UIKit;

namespace tSecretXamarin.iOS
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, "AppDelegate");
        }
    }
}
