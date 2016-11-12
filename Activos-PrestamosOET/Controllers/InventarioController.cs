using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Activos_PrestamosOET.Models;
using System.Data.Entity;
using System.Net;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.html.simpleparser;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using PagedList;

namespace Local.Controllers
{
    public class InventarioController : Controller
    {
        private Activos_PrestamosOET.Models.PrestamosEntities db = new Activos_PrestamosOET.Models.PrestamosEntities();

        // GET: Inventario/Index
        // default
        // Requiere: N/A.
        // Modifica: muestra la información al ingresar a la página de Inventario inicialmente.
        // Regresa: vista con la tabla de los datos acerca del Inventario.
        public ActionResult Index(int? page)
        {
            llenarTablaInventario();
            var temp = db.ACTIVOS.Where(x => x.PRESTABLE == true);
            temp = temp.OrderBy(s => s.FABRICANTE);
            int pageSize = 4;
            int pageNumber = (page ?? 1);
            return View(temp.ToPagedList(pageNumber, pageSize));
        }

        
        // Requiere: valor seleccionado en el dropdown de Categoría, valor del botón seleccionado, valor de la fecha inicial y la fecha final
        // Modifica: muestra después de seleccionado algún botón, los resultados correspondientes, mostrando las tablas que corresponden.
        // Regresa: vista con las tablas cargadas que corresponden.
        [HttpPost]
        public ActionResult Index(String dropdownCategoria, String submit, String datepicker, String datepicker1, string b, int? page)
        {
            if (!string.IsNullOrEmpty(submit) && submit.Equals("Buscar"))
            {
                if (!dropdownCategoria.Equals("1"))
                {
                    //se buscan los prestamos con la categoria seleccionada
                    llenarTabla(dropdownCategoria);

                    var temp = db.ACTIVOS.Where(x => (x.PRESTABLE == true) && (x.TIPOS_ACTIVOS.NOMBRE==dropdownCategoria));
                    temp = temp.OrderBy(s => s.FABRICANTE);
                    int pageSize = 4;
                    int pageNumber = (page ?? 1);
                    return View(temp.ToPagedList(pageNumber, pageSize));
                }//---------------------------------------------------------------------------------------------------------------------
                else
                {   
                    //se buscan los prestamos con todas las categorias
                    llenarTablaInventario();

                    var temp = db.ACTIVOS.Where(x => x.PRESTABLE == true);
                    temp = temp.OrderBy(s => s.FABRICANTE);
                    int pageSize = 4;
                    int pageNumber = (page ?? 1);
                    return View(temp.ToPagedList(pageNumber, pageSize));
                }
            }//--------------------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------------------
            else if (!string.IsNullOrEmpty(datepicker))
            {
                llenarTablaCategoria(datepicker, datepicker1, dropdownCategoria);

                var temp = db.ACTIVOS.Where(x => x.PRESTABLE == true);
                temp = temp.OrderBy(s => s.FABRICANTE);
                int pageSize = 4;
                int pageNumber = (page ?? 1);
                return View(temp.ToPagedList(pageNumber, pageSize));
            }
            else {
                return RedirectToAction("Inventario");
            }

        }

        // Requiere: N/A.
        // Modifica: se encarga de llenar la tabla de Inventario, de todas las categorías para ACTIVOS.
        // Regresa: N/A.
        private void llenarTablaInventario() {
            int cont = 0;
            var acts = db.ACTIVOS;
            foreach (Activos_PrestamosOET.Models.ACTIVO x in acts)
            {
                if (x.PRESTABLE == true)
                {
                    cont++;
                }
            }
            if (cont== 0)
            {
                ViewBag.Mensaje0 = "No hay Activos Prestables.";
            }
        }

        // Requiere: valor seleccionado en el dropdown de Categoría, valor del botón seleccionado, valor de la fecha inicial y la fecha final
        // Modifica: se encarga de llenar la tabla de Inventario, de la categoría que recibe cómo parámetro.
        // Regresa: N/A.
        private void llenarTabla(String dropdownCategoria)
        {
            int cont = 0;
            var acts = db.ACTIVOS;
            foreach (Activos_PrestamosOET.Models.ACTIVO x in acts)
            {
                if (x.PRESTABLE == true && x.TIPOS_ACTIVOS.NOMBRE == dropdownCategoria)
                {
                    cont++;
                }
            }
            if (cont == 0)
            {
                ViewBag.Mensaje0 = "No hay Activos Prestables con esta categoría.";
            }
        }

        // Requiere: valor seleccionado en el dropdown de Categoría, valor del botón seleccionado, valor de la fecha inicial y la fecha final
        // Modifica: Se encarga de llenar las tablas de Inventario y la de categoría, basadas en las selecciones del usuario.
        // Regresa: N/A.
        private void llenarTablaCategoria(String datepicker, String datepicker1, String dropdownCategoria) {
            var courses = new List<List<String>>();
            var prestamos = (from u in db.PRESTAMOS select u);
            Dictionary<string, decimal> dictionary = new Dictionary<string, decimal>();


            foreach (PRESTAMO p in prestamos) {
                DateTime dt = DateTime.ParseExact(datepicker, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                DateTime dt1 = DateTime.ParseExact(datepicker1, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                if (p.FECHA_SOLICITUD > dt && p.FECHA_SOLICITUD < dt1) {
                    foreach (EQUIPO_SOLICITADO e in p.EQUIPO_SOLICITADO) {
                        if (e.TIPOS_ACTIVOS != null) {
                            if (dictionary.ContainsKey(e.TIPOS_ACTIVOS.NOMBRE))
                            {
                                decimal value = dictionary[e.TIPOS_ACTIVOS.NOMBRE];
                                value += e.CANTIDAD;
                                dictionary[e.TIPOS_ACTIVOS.NOMBRE] = value;
                            }
                            else
                            {
                                dictionary.Add(e.TIPOS_ACTIVOS.NOMBRE, e.CANTIDAD);
                            }
                        }
                    }
                }
            }
            string[] keys;
            decimal[] values;
            
            keys = dictionary.Keys.ToArray();
            values = dictionary.Values.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                List<String> temp = new List<String>();
                temp.Add(keys[i]);
                temp.Add(values[i].ToString());
                courses.Add(temp);
            }
            if (keys.Length == 0)
            {
                ViewBag.Mensaje2 = "No hay préstamos en estas fechas.";
            }

            ViewBag.Courses1 = courses;
            llenarTablaInventario();
        }

        //Requiere: Recibe la placa del activo que se está consultando.
        // Modifica: Maneja el details view, la cual es la vista de consulta del tracking de un activo en particular.
        //Retorna: Devuelve un información necesaria para el despliegue de la vista como: nombre de solicitante, numero de boleta, observaciones al devolver, etc.
        [HttpGet]
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var activo = db.ACTIVOS.Include(p => p.PRESTAMOes).Include(p => p.TRANSACCIONES).SingleOrDefault(m => m.PLACA == id);

            return View(activo);
        }

        public ActionResult DescargarHistorial(string id)
        {
            var activo = db.ACTIVOS.Include(p => p.PRESTAMOes).Include(p => p.TRANSACCIONES).SingleOrDefault(m => m.PLACA == id);
            DownloadPDF("DetailsPDF", activo, "HistorialActivo");
                 return RedirectToAction("Details", new { id = id });

        }

        // Requiere: la placa del activo seleccionado desde la interfaz del historial
        // Modifica: Crea la vista de DetailsPDF. El usuario no va a ver esta vista, si no que es para despues convertirla en un string y poder crear el PDF del tracking del activo.
        // Regresa: N/A.
        public ActionResult DetailsPDF(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var activo = db.ACTIVOS.Include(p => p.PRESTAMOes).Include(p => p.TRANSACCIONES).SingleOrDefault(m => m.PLACA == id);

            //Si se presiona el boton de descargar la boleta

            return View(activo);

        }

        // Requiere: la placa del activo seleccionado desde la interfaz del historial
        // Modifica: permite hacer la busqueda del tracking del activo para poder convertirlo a excel.
        // Regresa: N/A.
        public ActionResult ExportarExcel(string id)
        {
            var activo = db.ACTIVOS.Include(p => p.PRESTAMOes).Include(p => p.TRANSACCIONES).SingleOrDefault(m => m.PLACA == id);
            var temp = activo.PRESTAMOes;
            var temp2 = activo.TRANSACCIONES;

            var grid = new GridView();
            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("Número de Boleta", Type.GetType("System.String")));
            dt.Columns.Add(new DataColumn("Fecha de Retiro", Type.GetType("System.String")));
            dt.Columns.Add(new DataColumn("Fecha de Devolución", Type.GetType("System.String")));
            dt.Columns.Add(new DataColumn("Solicitante", Type.GetType("System.String")));
            dt.Columns.Add(new DataColumn("Observaciones al devolver", Type.GetType("System.String")));

            foreach (var item in temp)
            {
                DataRow dr = dt.NewRow();
                if (item.NUMERO_BOLETA == null)
                {
                    dr["Número de Boleta"] = "No tiene Número de Boleta especificado";
                }
                else
                {
                    dr["Número de Boleta"] = item.NUMERO_BOLETA;
                }
                if (item.FECHA_RETIRO == null)
                {
                    dr["Fecha de Retiro"] = "No tiene Fecha de Retiro especificado";
                }
                else
                {
                    dr["Fecha de Retiro"] = item.FECHA_RETIRO.ToShortDateString();
                }
                if (item.FECHA_RETIRO == null)
                {
                    dr["Fecha de Devolución"] = "No tiene Fecha de Devolución especificada";
                }
                else
                {
                    dr["Fecha de Devolución"] = item.FECHA_RETIRO.AddDays(item.PERIODO_USO).ToShortDateString();
                }
                if (item.ActivosUser.Nombre == null)
                {
                    dr["Solicitante"] = "No tiene Solicitante especificado";
                }
                else
                {
                    dr["Solicitante"] = item.ActivosUser.Nombre;
                }
                foreach (var x in activo.TRANSACCIONES)
                {

                    if (x.ACTIVOID == activo.ID && x.NUMERO_BOLETA == item.NUMERO_BOLETA)
                    {
                        dr["Observaciones al devolver"] = x.OBSERVACIONES_RECIBO;

                    }
                    else if (x == null)
                    {
                        dr["Observaciones al devolver"] = "No ha sido prestado";
                    }

                }

                dt.Rows.Add(dr);
            }
            DataSet ds = new DataSet();
            ds.Tables.Add(dt);
            grid.DataSource = ds.Tables[0];
            grid.DataBind();

            Response.ClearContent();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment; filename=HistorialActivo.xls");
            Response.ContentType = "application/ms-excel";

            Response.Charset = "";
            StringWriter sw = new StringWriter();
            HtmlTextWriter htw = new HtmlTextWriter(sw);

            grid.RenderControl(htw);

            Response.Output.Write(sw.ToString());
            Response.Flush();
            Response.End();

            return View(temp);

        }

        //Requiere: N/A
        //Modifica: permite llamar el metodo que crea el PDF para ser bajado.
        //Regresa: vista de Inventario, una vez creado y bajado el Reporte en PDF.
        public ActionResult PDFReporte() {
            var temp = db.ACTIVOS.Where(x => x.PRESTABLE == true).ToList();
            DownloadPDF("BoletaPDF", temp, "BoletaSoliciud");
            return RedirectToAction("Inventario");
        }

        //Requiere: N/A
        //Modifica: constituye la vista que se convertira en PDF
        //Regresa: vista con el contenido del PDF
        public ActionResult BoletaPDF()
        {
            var temp = db.ACTIVOS.Where(x => x.PRESTABLE == true).ToList();

            return View(temp);
        }
        
        //Requere: N/A
        //Modifica: crea el Excel a ser descargado,con la informacion dentro de un gridview, en un excel
        //Regresa: vista con el Excel descargado
        public ActionResult ExportToExcel()
        {
            var temp = db.ACTIVOS.Where(x => x.PRESTABLE == true).ToList();

            var grid = new GridView();
            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("Fabricante", Type.GetType("System.String")));
            dt.Columns.Add(new DataColumn("Modelo", Type.GetType("System.String")));
            dt.Columns.Add(new DataColumn("Placa", Type.GetType("System.String")));
            dt.Columns.Add(new DataColumn("Tipo", Type.GetType("System.String")));
            dt.Columns.Add(new DataColumn("Descripcion", Type.GetType("System.String")));
            dt.Columns.Add(new DataColumn("Prestado_a", Type.GetType("System.String")));
            dt.Columns.Add(new DataColumn("Prestado_hasta", Type.GetType("System.String")));

            foreach (var item in temp)
            {
                DataRow dr = dt.NewRow();
                if (item.FABRICANTE == null)
                {
                    dr["Fabricante"] = "No tiene fabricante especificado";
                }
                else
                {
                    dr["Fabricante"] = item.FABRICANTE;
                }
                if (item.MODELO == null)
                {
                    dr["Modelo"] = "No tiene modelo especificado";
                }
                else
                {
                    dr["Modelo"] = item.MODELO;
                }
                if (item.PLACA == null)
                {
                    dr["Placa"] = "No tiene placa especificada";
                }
                else
                {
                    dr["Placa"] = item.PLACA;
                }
                if (item.TIPOS_ACTIVOS == null)
                {
                    dr["Tipo_activo"] = "No tiene Tipo de Activo especificado";
                }
                else
                {
                    dr["Tipo"] = item.TIPOS_ACTIVOS.NOMBRE;
                }
                if (item.DESCRIPCION == null)
                {
                    dr["Descripcion"] = "No tiene descripcion especificada";
                }
                else
                {
                    dr["Descripcion"] = item.DESCRIPCION;
                }
                foreach (var x in item.PRESTAMOes)
                {
                    if (x == null)
                    {
                        dr["Prestado_a"] = "No ha sido prestado";
                        dr["Prestado_hasta"] = "No ha sido prestado";
                    }
                    else
                    {
                        dr["Prestado_a"] = x.ActivosUser.Nombre;
                        dr["Prestado_hasta"] = x.FECHA_RETIRO.ToShortDateString();
                    }
                }
                dt.Rows.Add(dr);
            }
            DataSet ds = new DataSet();
            ds.Tables.Add(dt);
            grid.DataSource = ds.Tables[0];
            grid.DataBind();

            Response.ClearContent();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment; filename=ReporteActivos.xls");
            Response.ContentType = "application/ms-excel";

            Response.Charset = "";
            StringWriter sw = new StringWriter();
            HtmlTextWriter htw = new HtmlTextWriter(sw);

            grid.RenderControl(htw);

            Response.Output.Write(sw.ToString());
            Response.Flush();
            Response.End();

            return View();
        }


        //------------------------------------------------------------------------------------------------------

        //Requiere: la vista, el modelo al que pertenece la vista, el nombre que se quiere que tenga el archivo
        //Modifica: se encarga de organizar la entrada de una vista, llamar al metodo que lo convierte a un doc itextsharp y que se pueda descargar como PDF
        //Regresa: N/A

        public void DownloadPDF(string viewName, object model, string nombreArchivo)
        {
            string HTMLContent = RenderRazorViewToString(viewName, model);

            Response.Clear();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-disposition", "attachment;filename=" + nombreArchivo + ".pdf");
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.BinaryWrite(GetPDF(HTMLContent));
            Response.End();
        }

        //Requiere: la vista, el modelo al que pertenece la vista
        //Modifica: convierte la vista en un string para que pueda ser leido por itextsharp
        //Regresa: un string con la informacion de la vista
        public string RenderRazorViewToString(string viewName, object model)
        {
            // PRESTAMO pRESTAMO = db.PRESTAMOS.Find();
            ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext,viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View,ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }

        //Requiere: una string parseada de la vista que se quiere convertir en PDF
        //Modifica: se encarga de pasar la vista HTML a un documento de itextsharp
        //Regresa: un byte con el documento 
        public byte[] GetPDF(string pHTML)
        {
            byte[] bPDF = null;

            MemoryStream ms = new MemoryStream();
            TextReader txtReader = new StringReader(pHTML);

            // Se crea un documneto de itextsharp
            Document doc = new Document(PageSize.A4, 25, 25, 25, 25);

            PdfWriter oPdfWriter = PdfWriter.GetInstance(doc, ms);

            // el htmlworker parsea el documento
            HTMLWorker htmlWorker = new HTMLWorker(doc);

            doc.Open();
            htmlWorker.StartDocument();

            // parsea el html en el doc
            htmlWorker.Parse(txtReader);

            htmlWorker.EndDocument();
            htmlWorker.Close();
            doc.Close();

            bPDF = ms.ToArray();

            return bPDF;
        }

    }
}