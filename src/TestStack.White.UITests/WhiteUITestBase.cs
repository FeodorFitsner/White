﻿using Castle.Core.Logging;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using TestStack.White.Configuration;
using TestStack.White.InputDevices;
using TestStack.White.ScreenObjects;
using TestStack.White.UIItems;
using TestStack.White.UIItems.WindowItems;
using TestStack.White.UITests.Infrastructure;
using TestStack.White.UITests.Screens;

namespace TestStack.White.UITests
{
    public abstract class WhiteUITestBase : IDisposable
    {
        private readonly WindowsFramework framework;
        private IDisposable mainWindow;

        protected WhiteUITestBase(WindowsFramework framework)
        {
            this.framework = framework;
            CoreAppXmlConfiguration.Instance.LoggerFactory = new ConsoleFactory(LoggerLevel.Debug);
            screenshotDir = @"c:\FailedTestsScreenshots";
            Directory.CreateDirectory(screenshotDir);
        }

        [TearDown]
        public void TestTeardown()
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                TakeScreenshot(TestContext.CurrentContext.Test.FullName);
            }
        }

        [OneTimeSetUp]
        public void Setup()
        {
            mainWindow = SetMainWindow(framework);
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            mainWindow.Dispose();
        }

        readonly ILogger logger = CoreAppXmlConfiguration.Instance.LoggerFactory.Create(typeof(WhiteUITestBase));
        readonly List<Window> windowsToClose = new List<Window>();
        readonly string screenshotDir;

        internal Keyboard Keyboard;

        protected Window MainWindow { get; private set; }
        protected MainScreen MainScreen { get; private set; }
        protected Application Application { get; private set; }
        protected ScreenRepository Repository { get; private set; }

        private string TakeScreenshot(string screenshotName)
        {
            var imagename = screenshotName + ".png";
            var imagePath = Path.Combine(screenshotDir, imagename);
            try
            {
                new ScreenCapture().CaptureScreenShot().Save(imagePath, ImageFormat.Png);
                Trace.WriteLine(String.Format("Screenshot taken: {0}", imagePath));
            }
            catch (Exception)
            {
                Trace.TraceError(String.Format("Failed to save screenshot to directory: {0}, filename: {1}", screenshotDir, imagePath));
            }
            return imagePath;
        }

        private IDisposable SetMainWindow(WindowsFramework framework)
        {
            try
            {
                Keyboard = Keyboard.Instance;
                var configuration = TestConfigurationFactory.Create(framework);
                Application = configuration.LaunchApplication();
                Repository = new ScreenRepository(Application);
                MainWindow = configuration.GetMainWindow(Application);
                MainScreen = configuration.GetMainScreen(Repository);

                return new ShutdownApplicationDisposable(this);
            }
            catch (Exception e)
            {
                logger.Error("Failed to launch application and get main window", e);
                if (Application != null)
                    Application.Close();
                throw;
            }
        }

        private class ShutdownApplicationDisposable : IDisposable
        {
            private readonly WhiteUITestBase testBase;

            public ShutdownApplicationDisposable(WhiteUITestBase testBase)
            {
                this.testBase = testBase;
            }

            public void Dispose()
            {
                foreach (var window in testBase.windowsToClose)
                {
                    if (!window.IsClosed)
                        window.Close();
                }
                testBase.windowsToClose.Clear();
                testBase.MainWindow.Close();
                testBase.Application.Dispose();
                testBase.Application = null;
                testBase.MainWindow = null;
            }
        }

        protected Window StartScenario(string scenarioButtonId, string windowTitle)
        {
            MainWindow.Get<Button>(scenarioButtonId).Click();
            var window = MainWindow.ModalWindow(windowTitle);
            windowsToClose.Add(window);
            return window;
        }

        protected void SelectOtherControls()
        {
            MainWindow.Tabs[0].SelectTabPage(2);
        }

        protected void SelectInputControls()
        {
            MainWindow.Tabs[0].SelectTabPage(1);
        }

        protected void SelectListControls()
        {
            MainWindow.Tabs[0].SelectTabPage(0);
        }

        protected void SelectDataGridTab()
        {
            MainWindow.Tabs[0].SelectTabPage(3);
        }

        protected void SelectPropertyGridTab()
        {
            MainWindow.Tabs[0].SelectTabPage(4);
        }
    }
}