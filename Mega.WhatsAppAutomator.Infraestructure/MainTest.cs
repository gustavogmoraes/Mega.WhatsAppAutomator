using System;
using System.IO;
using OpenQA.Selenium.Chrome;
using WebDriverManager;

namespace Mega.WhatsAppAutomator.Infraestructure
{
    public class MainTest
    {
        public void Test()
        {
            var options = new ChromeOptions();
            options.BinaryLocation = AppDomain.CurrentDomain.BaseDirectory;
            options.AddArguments("remote-debugging-port=12345");
            //options.AddArguments("headless", "disable-gpu", "no-sandbox", "disable-extensions"); // Headless
            options.AddArguments("--proxy-server='direct://'", "--proxy-bypass-list=*"); // Speed

            var driver = new ChromeDriver(options);
            driver.Navigate().GoToUrl("http://www.google.com/");
        }
    }
}