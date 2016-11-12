using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;

namespace UnitTestProject1.Controllers
{
    [TestClass]
    public class ActivosControllerTest: SeleniumTest
    {
        public ActivosControllerTest() : base("Activos-PrestamosOET") { }

        [TestMethod]
        public void LoginTest()
        {
            this.ChromeDriver.Navigate().GoToUrl(this.GetAbsoluteUrl("/Account/Login"));
            this.ChromeDriver.FindElementById("Email").SendKeys("andresbejar@gmail.com");
            this.ChromeDriver.FindElementById("Password").SendKeys("Snakeeater@333");
            this.ChromeDriver.FindElementById("Email").Submit(); //submit form

            WebDriverWait wait = new WebDriverWait(this.ChromeDriver, TimeSpan.FromSeconds(10));
            Boolean funciono = wait.Until(ExpectedConditions.TitleContains("Inicio"));
            Assert.IsTrue(funciono);

        }

        public void LoginSetup()
        {
            this.ChromeDriver.Navigate().GoToUrl(this.GetAbsoluteUrl("/Account/Login"));
            this.ChromeDriver.FindElementById("Email").SendKeys("andresbejar@gmail.com");
            this.ChromeDriver.FindElementById("Password").SendKeys("Snakeeater@333");
            this.ChromeDriver.FindElementById("Email").Submit(); //submit form

            WebDriverWait wait = new WebDriverWait(this.ChromeDriver, TimeSpan.FromSeconds(10));
            wait.Until(ExpectedConditions.TitleContains("Inicio"));
        }

        [TestMethod]
        public void IngresarActivosTest()
        {

            LoginSetup();
            this.ChromeDriver.Navigate().GoToUrl(this.GetAbsoluteUrl("/Activos/Create"));
            this.ChromeDriver.FindElementById("DESCRIPCION").SendKeys("Prueba Selenium");
            this.ChromeDriver.FindElement(By.Id("PLACA")).SendKeys("333333333");
            this.ChromeDriver.FindElementById("NUMERO_SERIE").SendKeys("333333333");
            this.ChromeDriver.FindElementById("MODELO").SendKeys("WebDriver");
            this.ChromeDriver.FindElementById("FABRICANTE").SendKeys("Selenium");
            this.ChromeDriver.FindElementById("PRECIO").SendKeys("25000");
            this.ChromeDriver.FindElementById("NUMERO_DOCUMENTO").SendKeys("33333333");

            //proveedor
            IWebElement select = this.ChromeDriver.FindElementById("V_PROVEEDORIDPROVEEDOR");
            SelectElement selector = new SelectElement(select);
            selector.SelectByIndex(5); //seleccionamos A.Y.A

            //fecha compra
            IWebElement calendario = this.ChromeDriver.FindElementById("FECHA_COMPRA");
            //calendario.Click();
            calendario.SendKeys("037"); //3 de julio
            calendario.SendKeys(Keys.Tab);
            calendario.SendKeys("2016");

            //tipo transaccion
            IWebElement tipo_transaccion = this.ChromeDriver.FindElementById("TIPO_TRANSACCIONID");
            selector = new SelectElement(tipo_transaccion);
            selector.SelectByIndex(1); //Compra

            //Compañia
            IWebElement anfitriona = this.ChromeDriver.FindElementById("V_ANFITRIONAID");
            selector = new SelectElement(anfitriona);
            selector.SelectByIndex(1); //ESINTRO

            //Tipo Activo
            IWebElement tipo_activo = this.ChromeDriver.FindElementById("TIPO_ACTIVOID");
            selector = new SelectElement(tipo_activo);
            selector.SelectByIndex(1); //computadora

            //this.ChromeDriver.FindElementByCssSelector("input .btn .btn-default").Click(); //se envia la info
            this.ChromeDriver.FindElementById("DESCRIPCION").Submit();
            WebDriverWait wait = new WebDriverWait(this.ChromeDriver, TimeSpan.FromSeconds(10));
            Boolean funciono = wait.Until(ExpectedConditions.TitleContains("Index"));
            Assert.IsTrue(funciono);
             
        }

        [TestMethod]
        public void BorrarActivoTest()
        {
            LoginSetup();

            //primero tengo que ir al index
            this.ChromeDriver.Navigate().GoToUrl(this.GetAbsoluteUrl("/Activos"));
            WebDriverWait wait = new WebDriverWait(this.ChromeDriver, TimeSpan.FromSeconds(10));
            Boolean funciono = wait.Until(ExpectedConditions.TitleContains("Index"));

            //ahora tengo que buscar el activo que cree anteriormente
            //agarro la tabla
            IWebElement tabla = this.ChromeDriver.FindElement(By.ClassName("table"));

            //agarro todas las filas
            IList<IWebElement> filas = tabla.FindElements(By.XPath("tbody/tr"));
            IList<IWebElement> data;
            IWebElement link = null;
            foreach(var row in filas)
            {
                data = row.FindElements(By.TagName("td"));

                System.Diagnostics.Debug.Print(data[4].Text);
                if (data[4].Text.Equals("Prueba Selenium"))
                {
                    link = data[8].FindElement(By.TagName("a"));
                    System.Diagnostics.Debug.Print(link.GetAttribute("href"));
                    
                    break;
                }
            }
            //encontre el link, dele click
            //Assert.IsNotNull(link);
            link.Click();
            wait = new WebDriverWait(this.ChromeDriver, TimeSpan.FromSeconds(10));
            wait.Until(ExpectedConditions.TitleContains("Borrado"));
            this.ChromeDriver.FindElementByCssSelector("[class*='btn-default']").Click();

            wait = new WebDriverWait(this.ChromeDriver, TimeSpan.FromSeconds(10));
            funciono = wait.Until(ExpectedConditions.TitleContains("Index"));
            Assert.IsTrue(funciono);
        }
    }
}
