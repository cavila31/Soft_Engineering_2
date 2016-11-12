using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using SendGrid;
using System.Web;
using System.Web.Mvc;
using Activos_PrestamosOET.Models;
using PagedList;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System.Configuration;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.IO;

namespace Activos_PrestamosOET.Controllers
{
    [Authorize]
    public class ActivosController : Controller
    {
        private PrestamosEntities db = new PrestamosEntities();
        private TransaccionesController controladora_transaccion = new TransaccionesController();

        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public ActivosController() { }

        public ActivosController(ApplicationUserManager userManager)
        {
            this.UserManager = userManager;
        }

        [Authorize(Roles = "Ingresar activos, Desechar activos, Editar activos, Aceptar préstamos, superadmin")]
        // GET: Inventario
        public ActionResult Inventario(string orden, int? pagina, string busqueda)
        {
            ViewBag.OrdenActual = orden;
            ViewBag.Compania = String.IsNullOrEmpty(orden) ? "compania_desc" : "";
            ViewBag.Estacion = (orden == "estacion_asc") ? "estacion_desc" : "estacion_asc";
            ViewBag.Tipo = (orden == "tipo_asc") ? "tipo_desc" : "tipo_asc";
            ViewBag.Responsable = (orden == "responsable_asc") ? "responsable_desc" : "responsable_asc";
            ViewBag.Descripcion = (orden == "descrip_asc") ? "descrip_desc" : "descrip_asc";


            //se obtiene el usuario loggeado
            var user = UserManager.FindById(User.Identity.GetUserId());
            Boolean isAdmin = User.IsInRole("superadmin") ? true : false;
            IQueryable<ACTIVO> aCTIVOS = ACTIVO.busquedaSimple(busqueda, user.EstacionID, isAdmin);

            switch (orden)
            {
                case "compania_desc":
                    aCTIVOS = aCTIVOS.OrderByDescending(a => a.V_ANFITRIONA.SIGLAS);
                    break;
                case "estacion_asc":
                    aCTIVOS = aCTIVOS.OrderBy(a => a.V_ESTACION.SIGLAS);
                    break;
                case "estacion_desc":
                    aCTIVOS = aCTIVOS.OrderByDescending(a => a.V_ESTACION.SIGLAS);
                    break;
                case "tipo_asc":
                    aCTIVOS = aCTIVOS.OrderBy(a => a.TIPOS_ACTIVOS.NOMBRE);
                    break;
                case "tipo_desc":
                    aCTIVOS = aCTIVOS.OrderByDescending(a => a.TIPOS_ACTIVOS.NOMBRE);
                    break;
                case "responsable_asc":
                    aCTIVOS = aCTIVOS.OrderBy(a => a.V_EMPLEADOS.NOMBRE);
                    break;
                case "responsable_desc":
                    aCTIVOS = aCTIVOS.OrderByDescending(a => a.V_EMPLEADOS.NOMBRE);
                    break;
                case "descrip_desc":
                    aCTIVOS = aCTIVOS.OrderByDescending(a => a.DESCRIPCION);
                    break;
                case "descrip_asc":
                    aCTIVOS = aCTIVOS.OrderBy(a => a.DESCRIPCION);
                    break;
                default:
                    aCTIVOS = aCTIVOS.OrderBy(a => a.V_ANFITRIONA.SIGLAS);
                    break;
            }

            int tamano_pagina = 20;
            int num_pagina = (pagina ?? 1);
            return View(aCTIVOS.ToPagedList(num_pagina, tamano_pagina));
        }

        // POST que genera un PDF con codigos de barras
        [HttpPost]
        public ActionResult GenerarPDFCodigoBarras(string[] marcados, int codigo_seleccionado = 0)
        {
            string nombre_archivo = "Códigos " + DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt");
            marcados = marcados.Where(val => val != "false").ToArray();
            Response.Clear();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-disposition", "attachment;filename=" + nombre_archivo + ".pdf");
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.BinaryWrite(CrearPDF(marcados, codigo_seleccionado));
            Response.End();
            return RedirectToAction("Inventario");
        }

        /**
         * Metodo que se encarga de generar un PDF ya sean con codigos QR o de Barras dependiendo de lo que haya elegido el usuario.
         **/
        private byte[] CrearPDF(string[] datos, int tipo)
        {
            byte[] bPDF = null;

            MemoryStream ms = new MemoryStream();
            Document doc = new Document(PageSize.A4, 25, 25, 25, 25);
            PdfWriter writer = PdfWriter.GetInstance(doc, ms);
            doc.Open();
            switch (tipo)
            { //Escoger tipo de PDF
                case 1:
                    doc.Add(CodigoQR(datos, 6));
                    break;
                default:
                    doc.Add(Codigo39(datos, writer, 6));
                    break;
            }
            doc.Close();
            bPDF = ms.ToArray();
            return bPDF;
        }

        /**
         * Metodo que genera un PDF con codigos QR
         * **/
        private PdfPTable CodigoQR(string[] datos, int codigos_por_fila)
        {
            PdfPTable table = new PdfPTable(codigos_por_fila);
            table.WidthPercentage = 100;
            //****Se agregan elementos 
            for (int i = 0; i < datos.Length; i++)
            {
                BarcodeQRCode bc = new BarcodeQRCode(datos[i], 1, 1, null);
                PdfPCell cell = new PdfPCell(table.DefaultCell);
                cell.AddElement(new Chunk("OET - " + datos[i]));
                cell.AddElement(bc.GetImage());
                table.AddCell(cell);
            }
            table.CompleteRow();
            return table;
        }

        /**
         * Metodo que genera un PDF con codigos de barras.
         * **/
        private PdfPTable Codigo39(string[] datos, PdfWriter writer, int codigos_por_fila)
        {
            PdfPTable table = new PdfPTable(codigos_por_fila);
            table.WidthPercentage = 100;
            //****Se agregan elementos 
            for (int i = 0; i < datos.Length; i++)
            {
                Barcode39 bc = new Barcode39();
                PdfContentByte cb = new PdfContentByte(writer);
                bc.Code = datos[i];
                bc.N = 3;
                bc.X = 1;
                bc.AltText = "OET - " + datos[i].ToString();
                PdfPCell cell = new PdfPCell(table.DefaultCell);
                cell.AddElement(bc.CreateImageWithBarcode(cb, null, null));
                table.AddCell(cell);
            }
            table.CompleteRow();
            return table;
        }




        // GET: Activos
        [Authorize(Roles = "Ingresar activos, Desechar activos, Editar activos, Aceptar préstamos, superadmin")]
        public ActionResult Index(string orden, string filtro, string busqueda, string V_PROVEEDORIDPROVEEDOR, string TIPO_ACTIVOID, string V_ANFITRIONAID, string TIPO_TRANSACCIONID, string ESTADO_ACTIVOID, string V_ESTACIONID, string fecha_antes, string fecha_despues, string usuario, string fabricante, int? pagina)
        {

            ViewBag.OrdenActual = orden;
            ViewBag.Compania = String.IsNullOrEmpty(orden) ? "compania_desc" : "";
            ViewBag.Estacion = (orden == "estacion_asc") ? "estacion_desc" : "estacion_asc";
            ViewBag.Tipo = (orden == "tipo_asc") ? "tipo_desc" : "tipo_asc";
            ViewBag.Responsable = (orden == "responsable_asc") ? "responsable_desc" : "responsable_asc";
            ViewBag.Descripcion = (orden == "descrip_asc") ? "descrip_desc" : "descrip_asc";
            //se obtiene el usuario loggeado
            var user = UserManager.FindById(User.Identity.GetUserId());
            Boolean isAdmin = User.IsInRole("superadmin") ? true : false;
            // Paginación
            if (busqueda != null)
            {
                pagina = 1;
            }
            else
            {
                busqueda = filtro;
            }

            ViewBag.FiltroActual = busqueda;

            // Busqueda con base en los parametros que ingresa el usuario
            #region Busqueda simple
            IQueryable<ACTIVO> aCTIVOS = ACTIVO.busquedaSimple(busqueda, user.EstacionID, isAdmin); //OJO
            #endregion

            #region Busqueda avanzada

            // Para las opciones de busqueda avanzada
            ViewBag.TIPO_TRANSACCIONID = new SelectList(db.TIPOS_TRANSACCIONES, "ID", "NOMBRE");
            ViewBag.TIPO_ACTIVOID = new SelectList(db.TIPOS_ACTIVOS, "ID", "NOMBRE");
            ViewBag.V_PROVEEDORIDPROVEEDOR = new SelectList(db.V_PROVEEDOR, "IDPROVEEDOR", "NOMBRE");
            ViewBag.V_ANFITRIONAID = new SelectList(db.V_ANFITRIONA, "ID", "NOMBRE");
            ViewBag.ESTADO_ACTIVOID = new SelectList(db.ESTADOS_ACTIVOS, "ID", "NOMBRE");
            ViewBag.V_ESTACIONID = new SelectList(db.V_ESTACION, "ID", "NOMBRE");

            Dictionary<string, string> params_busqueda = new Dictionary<string, string>();

            params_busqueda.Add("proveedor", V_PROVEEDORIDPROVEEDOR);
            params_busqueda.Add("tipo_activo", TIPO_ACTIVOID);
            params_busqueda.Add("anfitriona", V_ANFITRIONAID);
            params_busqueda.Add("tipo_transaccion", TIPO_TRANSACCIONID);
            params_busqueda.Add("fecha_antes", fecha_antes);
            params_busqueda.Add("fecha_despues", fecha_despues);
            params_busqueda.Add("usuario", usuario);
            params_busqueda.Add("estado_activo", ESTADO_ACTIVOID);
            params_busqueda.Add("estacion", V_ESTACIONID);
            params_busqueda.Add("fabricante", fabricante);

            aCTIVOS = ACTIVO.busquedaAvanzada(params_busqueda, aCTIVOS);
            #endregion

            switch (orden)
            {
                case "compania_desc":
                    aCTIVOS = aCTIVOS.OrderByDescending(a => a.V_ANFITRIONA.SIGLAS);
                    break;
                case "estacion_asc":
                    aCTIVOS = aCTIVOS.OrderBy(a => a.V_ESTACION.SIGLAS);
                    break;
                case "estacion_desc":
                    aCTIVOS = aCTIVOS.OrderByDescending(a => a.V_ESTACION.SIGLAS);
                    break;
                case "tipo_asc":
                    aCTIVOS = aCTIVOS.OrderBy(a => a.TIPOS_ACTIVOS.NOMBRE);
                    break;
                case "tipo_desc":
                    aCTIVOS = aCTIVOS.OrderByDescending(a => a.TIPOS_ACTIVOS.NOMBRE);
                    break;
                case "responsable_asc":
                    aCTIVOS = aCTIVOS.OrderBy(a => a.V_EMPLEADOS.NOMBRE);
                    break;
                case "responsable_desc":
                    aCTIVOS = aCTIVOS.OrderByDescending(a => a.V_EMPLEADOS.NOMBRE);
                    break;
                case "descrip_desc":
                    aCTIVOS = aCTIVOS.OrderByDescending(a => a.DESCRIPCION);
                    break;
                case "descrip_asc":
                    aCTIVOS = aCTIVOS.OrderBy(a => a.DESCRIPCION);
                    break;
                default:
                    aCTIVOS = aCTIVOS.OrderBy(a => a.V_ANFITRIONA.SIGLAS);
                    break;
            }

            int tamano_pagina = 20;
            int num_pagina = (pagina ?? 1);

            return View(aCTIVOS.ToPagedList(num_pagina, tamano_pagina));
        }

        // GET: Activos/Details/5
        [Authorize(Roles = "Ingresar activos, Desechar activos, Editar activos, Aceptar préstamos, superadmin")]
        public ActionResult Details(string id, bool reparaciones = false)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ACTIVO aCTIVO = db.ACTIVOS.Find(id);

            if (aCTIVO == null)
            {
                return HttpNotFound();
            }

            aCTIVO.TRANSACCIONES = aCTIVO.TRANSACCIONES.Where(a => a.ACTIVOID.Equals(id)).ToList();
            if (reparaciones)
                aCTIVO.TRANSACCIONES = aCTIVO.TRANSACCIONES.Where(a => a.ESTADO.Equals("Dañado sin reparación") || a.ESTADO.Equals("En reparación")).ToList();
            aCTIVO.TRANSACCIONES = aCTIVO.TRANSACCIONES.OrderByDescending(a => a.FECHA).ToList();

            return View(aCTIVO);
        }

        // GET: Activos/Create
        [Authorize(Roles = "Ingresar activos, superadmin")] //OJO
        public ActionResult Create()
        {
            ViewBag.INGRESADO_POR = User.Identity.Name;
            ViewBag.TIPO_TRANSACCIONID = new SelectList(db.TIPOS_TRANSACCIONES.OrderBy(tt => tt.NOMBRE), "ID", "NOMBRE");
            ViewBag.TIPO_ACTIVOID = new SelectList(db.TIPOS_ACTIVOS.OrderBy(ta => ta.NOMBRE), "ID", "NOMBRE");
            ViewBag.V_PROVEEDORIDPROVEEDOR = new SelectList(db.V_PROVEEDOR.OrderBy(p => p.NOMBRE), "IDPROVEEDOR", "NOMBRE");
            ViewBag.V_ANFITRIONAID = new SelectList(db.V_ANFITRIONA.OrderBy(a => a.NOMBRE), "ID", "NOMBRE");
            ViewBag.FECHA_INGRESO = DateTime.Now.ToString("yyyy-MM-dd");
            ViewBag.V_MONEDAID = new SelectList(db.V_MONEDA, "ID", "SIMBOLO");

            return View();
        }

        // POST: Activos/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Ingresar activos, superadmin")]
        public ActionResult Create([Bind(Include = "ID,NUMERO_SERIE,FECHA_COMPRA,INICIO_SERVICIO,FECHA_INGRESO,FABRICANTE,PRECIO,DESCRIPCION,EXENTO,PRESTABLE,TIPO_CAPITAL,INGRESADO_POR,NUMERO_DOCUMENTO,NUMERO_LOTE,TIPO_TRANSACCIONID,ESTADO_ACTIVOID,TIPO_ACTIVOID,COMENTARIO,DESECHADO,MODELO,V_EMPLEADOSIDEMPLEADO,V_ESTACIONID,V_ANFITRIONAID,V_PROVEEDORIDPROVEEDOR,V_MONEDAID,CENTRO_DE_COSTOId,PLACA,ESTADO_PRESTADO")] ACTIVO aCTIVO)
        {

            var estado = db.ESTADOS_ACTIVOS.ToList().Where(ea => ea.NOMBRE == "Disponible");
            aCTIVO.ESTADO_ACTIVOID = estado.ToList()[0].ID;
            aCTIVO.INGRESADO_POR = User.Identity.Name;
            decimal precio;
            if (db.V_MONEDA.Find(Request["V_MONEDAID"]).NOMBRE.Equals("Colones"))
            {
                // Colones
                decimal tipo_cambio = db.V_TIPO_CAMBIO.ToList()[0].TIPOCAMBIO;
                precio = aCTIVO.PRECIO / tipo_cambio;

            }
            else
            {
                //Dolares
                precio = aCTIVO.PRECIO;

            }
            aCTIVO.TIPO_CAPITAL = (precio >= 1000) ? true : false;

            if (ModelState.IsValid)
            {
                db.ACTIVOS.Add(aCTIVO);
                db.SaveChanges();


                var consulta_proveedor = db.V_PROVEEDOR.ToList().Where(ea => ea.IDPROVEEDOR == aCTIVO.V_PROVEEDORIDPROVEEDOR);
                var proveedor = consulta_proveedor.ToList()[0].NOMBRE;
                var consulta_anfitriona = db.V_ANFITRIONA.ToList().Where(ea => ea.ID == aCTIVO.V_ANFITRIONAID);
                var anfitriona = consulta_anfitriona.ToList()[0].NOMBRE;
                var consulta_transaccion = db.TIPOS_TRANSACCIONES.ToList().Where(ea => ea.ID == aCTIVO.TIPO_TRANSACCIONID);
                var transaccion = consulta_transaccion.ToList()[0].NOMBRE;

                controladora_transaccion.Create(User.Identity.GetUserName(), "Creado", aCTIVO.descripcion(proveedor, transaccion, anfitriona), aCTIVO.ID);
                return RedirectToAction("Index");
            }

            ViewBag.TIPO_TRANSACCIONID = new SelectList(db.TIPOS_TRANSACCIONES.OrderBy(tt => tt.NOMBRE), "ID", "NOMBRE", aCTIVO.TIPO_TRANSACCIONID);
            ViewBag.TIPO_ACTIVOID = new SelectList(db.TIPOS_ACTIVOS.OrderBy(ta => ta.NOMBRE), "ID", "NOMBRE", aCTIVO.TIPO_ACTIVOID);
            ViewBag.V_PROVEEDORIDPROVEEDOR = new SelectList(db.V_PROVEEDOR.OrderBy(p => p.NOMBRE), "IDPROVEEDOR", "NOMBRE", aCTIVO.V_PROVEEDORIDPROVEEDOR);
            ViewBag.V_ANFITRIONAID = new SelectList(db.V_ANFITRIONA.OrderBy(a => a.NOMBRE), "ID", "NOMBRE", aCTIVO.V_ANFITRIONAID);
            ViewBag.V_MONEDAID = new SelectList(db.V_MONEDA, "ID", "SIMBOLO", aCTIVO.V_MONEDAID);
            ViewBag.FECHA_INGRESO = DateTime.Now.ToString("yyyy-MM-dd");
            ViewBag.INGRESADO_POR = User.Identity.Name;
            return View(aCTIVO);
        }

        // GET: Activos/Asignar/7
        [Authorize(Roles = "Ingresar activos, superadmin, Editar activos")]
        public ActionResult Asignar(string id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // no deberia cargar datos de la asignacion pasada, cada asignacion es nueva
            // si quiero ver a quien esta asignado nada mas puedo ver los detalles del activo
            ACTIVO aCTIVO = db.ACTIVOS.Find(id);
            if (aCTIVO.DESECHADO)
                return RedirectToAction("Index");

            aCTIVO.COMENTARIO = "";


            if (aCTIVO == null)
            {
                return HttpNotFound();
            }
            ViewBag.V_EMPLEADOSIDEMPLEADO = new SelectList(db.V_EMPLEADOS.Where(emp => emp.ESTADO.Equals(1) && emp.EMAIL.Contains("@")).OrderBy(emp => emp.NOMBRE), "IDEMPLEADO", "NOMBRE");
            ViewBag.ESTADO_ACTIVOID = new SelectList(db.ESTADOS_ACTIVOS.OrderBy(ea => ea.NOMBRE), "ID", "NOMBRE", aCTIVO.ESTADO_ACTIVOID);
            ViewBag.V_ESTACIONID = new SelectList(db.V_ESTACION.OrderBy(e => e.NOMBRE), "ID", "NOMBRE", aCTIVO.V_ESTACIONID);
            ViewBag.CENTRO_DE_COSTOId = new SelectList(db.CENTROS_DE_COSTOS, "ID", "NOMBRE", aCTIVO.CENTRO_DE_COSTOId);
            return View(aCTIVO);
        }

        // POST: Activos/Asignar/7
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Ingresar activos, superadmin. Editar activos")]
        public ActionResult Asignar([Bind(Include = "ID,NUMERO_SERIE,FECHA_COMPRA,INICIO_SERVICIO,FECHA_INGRESO,FABRICANTE,PRECIO,DESCRIPCION,EXENTO,PRESTABLE,TIPO_CAPITAL,INGRESADO_POR,NUMERO_DOCUMENTO,NUMERO_LOTE,TIPO_TRANSACCIONID,ESTADO_ACTIVOID,TIPO_ACTIVOID,COMENTARIO,DESECHADO,MODELO,V_EMPLEADOSIDEMPLEADO,V_ESTACIONID,V_ANFITRIONAID,V_PROVEEDORIDPROVEEDOR,V_MONEDAID,CENTRO_DE_COSTOId,PLACA,ESTADO_PRESTADO")] ACTIVO aCTIVO)
        {

            var original = db.ACTIVOS.Find(aCTIVO.ID);
            if (aCTIVO.DESECHADO)
                return RedirectToAction("Index");

            if (original != null)
            {
                original.INICIO_SERVICIO = aCTIVO.INICIO_SERVICIO;
                original.ESTADO_ACTIVOID = aCTIVO.ESTADO_ACTIVOID;
                // si no se cambio el comentario, dejar el original
                original.COMENTARIO = aCTIVO.COMENTARIO == null ? original.COMENTARIO : aCTIVO.COMENTARIO;
                // Si el activo se pone como asignado, agregar id de empleado encargado, id de estacion de epleado y centro de costo
                int id_estado_asignado = db.ESTADOS_ACTIVOS.Where(ea => ea.NOMBRE.Equals("Asignado")).ToList()[0].ID; // Se busca el identificador del estado Asignado
                if (aCTIVO.ESTADO_ACTIVOID == id_estado_asignado)
                {
                    original.V_EMPLEADOSIDEMPLEADO = aCTIVO.V_EMPLEADOSIDEMPLEADO;
                    // Al activo se le asigna la estacion del empleado encargado, para que siempre este correcta y no dependa de la correctitud del filtro de empleados por estacion.
                    original.V_ESTACIONID = (db.V_EMPLEADOS.ToList().Where(ea => ea.IDEMPLEADO == aCTIVO.V_EMPLEADOSIDEMPLEADO)).ToList()[0].ESTACION_ID;
                    original.CENTRO_DE_COSTOId = aCTIVO.CENTRO_DE_COSTOId;

                    var empleado = db.V_EMPLEADOS.Find(aCTIVO.V_EMPLEADOSIDEMPLEADO);

                    if (empleado.EMAIL.Contains("@"))
                    {
                        var mensaje_correo = new SendGridMessage();
                        mensaje_correo.From = new System.Net.Mail.MailAddress("message.ots@tropicalstudies.org", "Admin"); // CAMBIAR CON EL CORREO DESDE EL QUE SE VAN A ENVIAR LOS MENSAJES
                        List<String> destinatarios = new List<string>
                        {
                            @""+empleado.NOMBRE+" <"+empleado.EMAIL+">"
                            //@"Jose Urena <jpurena14@hotmail.com>" // PARA ESTABLECER EL CORREO DONDE SE ENVIA LA INFORMACION, ELIMINAR ESTA LINEA Y DESCOMENTAR LA LINEA SUPERIOR.
                        };

                        mensaje_correo.AddTo(destinatarios);
                        mensaje_correo.Subject = "Activo asignado a su cuenta de OET.";
                        mensaje_correo.Html += "<h2>La OET le informa</h2><br />";
                        mensaje_correo.Html += "A través de este correo la OET desea informarle que un nuevo activo ha sido asignado a su nombre." + "<br />" + "<br />";
                        mensaje_correo.Html += "A continuación se enlistan las características del activo: " + "<br />" + "<br />" + "<br />";
                        mensaje_correo.Html += "Descripción: " + original.DESCRIPCION + "<br />" + "<br />";
                        mensaje_correo.Html += "Número de placa: " + original.PLACA + "<br />" + "<br />";
                        mensaje_correo.Html += "Inicio del servicio: " + original.INICIO_SERVICIO + "<br />" + "<br />";
                        mensaje_correo.Html += "Comentarios: " + original.COMENTARIO + "<br />";

                        var credentials = new NetworkCredential(ConfigurationManager.AppSettings["mailAccount"], ConfigurationManager.AppSettings["mailPassword"]);
                        var transportWeb = new SendGrid.Web(credentials);
                        transportWeb.DeliverAsync(mensaje_correo);
                    }



                }
                else
                {
                    // Si no se asigna entonces se le quita la estacion, el centro de costo y el empleado responsable
                    original.V_ESTACIONID = "";
                    original.CENTRO_DE_COSTOId = null;
                    original.V_EMPLEADOSIDEMPLEADO = "";
                }

                db.SaveChanges();

                var consulta_proveedor = db.V_PROVEEDOR.ToList().Where(ea => ea.IDPROVEEDOR == original.V_PROVEEDORIDPROVEEDOR);
                var proveedor = consulta_proveedor.ToList()[0].NOMBRE;
                var consulta_anfitriona = db.V_ANFITRIONA.ToList().Where(ea => ea.ID == original.V_ANFITRIONAID);
                var anfitriona = consulta_anfitriona.ToList()[0].NOMBRE;
                var consulta_transaccion = db.TIPOS_TRANSACCIONES.ToList().Where(ea => ea.ID == original.TIPO_TRANSACCIONID);
                var transaccion = consulta_transaccion.ToList()[0].NOMBRE;

                // Si el activo se pone como asignado, se agrega id del responsable a la bitacora.
                if (aCTIVO.ESTADO_ACTIVOID == id_estado_asignado)
                {
                    controladora_transaccion.CreateWithResponsible(User.Identity.GetUserName(), original.ESTADOS_ACTIVOS.NOMBRE, original.descripcion(proveedor, transaccion, anfitriona), original.ID, aCTIVO.V_EMPLEADOSIDEMPLEADO);
                }
                else
                {
                    controladora_transaccion.Create(User.Identity.GetUserName(), original.ESTADOS_ACTIVOS.NOMBRE, original.descripcion(proveedor, transaccion, anfitriona), original.ID);
                }

                return RedirectToAction("Index");
            }
            ViewBag.V_EMPLEADOSIDEMPLEADO = new SelectList(db.V_EMPLEADOS.Where(emp => emp.ESTADO.Equals(1) && emp.EMAIL.Contains("@")).OrderBy(emp => emp.NOMBRE), "IDEMPLEADO", "NOMBRE", aCTIVO.V_EMPLEADOSIDEMPLEADO);
            ViewBag.ESTADO_ACTIVOID = new SelectList(db.ESTADOS_ACTIVOS.OrderBy(ea => ea.NOMBRE), "ID", "NOMBRE", aCTIVO.ESTADO_ACTIVOID);
            ViewBag.V_ESTACIONID = new SelectList(db.V_ESTACION.OrderBy(e => e.NOMBRE), "ID", "NOMBRE", aCTIVO.V_ESTACIONID);
            ViewBag.CENTRO_DE_COSTOId = new SelectList(db.CENTROS_DE_COSTOS, "ID", "NOMBRE", aCTIVO.CENTRO_DE_COSTOId);
            return View(aCTIVO);
        }

        // GET: Activos/Edit/5
        [Authorize(Roles = "Editar activos, superadmin")]
        public ActionResult Edit(string id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            ACTIVO aCTIVO = db.ACTIVOS.Find(id);
            if (aCTIVO.DESECHADO)
                return RedirectToAction("Index");
            if (aCTIVO == null)
                return HttpNotFound();

            ViewBag.TIPO_TRANSACCIONID = new SelectList(db.TIPOS_TRANSACCIONES.OrderBy(tt => tt.NOMBRE), "ID", "NOMBRE", aCTIVO.TIPO_TRANSACCIONID);
            ViewBag.TIPO_ACTIVOID = new SelectList(db.TIPOS_ACTIVOS.OrderBy(ta => ta.NOMBRE), "ID", "NOMBRE", aCTIVO.TIPO_ACTIVOID);
            ViewBag.V_PROVEEDORIDPROVEEDOR = new SelectList(db.V_PROVEEDOR.OrderBy(p => p.NOMBRE), "IDPROVEEDOR", "NOMBRE", aCTIVO.V_PROVEEDORIDPROVEEDOR);
            ViewBag.V_ANFITRIONAID = new SelectList(db.V_ANFITRIONA.OrderBy(a => a.NOMBRE), "ID", "NOMBRE", aCTIVO.V_ANFITRIONAID);
            ViewBag.V_MONEDAID = new SelectList(db.V_MONEDA, "ID", "SIMBOLO", aCTIVO.V_MONEDAID);
            ViewBag.FECHA_INGRESO = aCTIVO.FECHA_INGRESO.Date;
            return View(aCTIVO);
        }

        // POST: Activos/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Editar activos, superadmin")]
        public ActionResult Edit([Bind(Include = "ID,NUMERO_SERIE,FECHA_COMPRA,INICIO_SERVICIO,FECHA_INGRESO,FABRICANTE,PRECIO,DESCRIPCION,EXENTO,PRESTABLE,TIPO_CAPITAL,INGRESADO_POR,NUMERO_DOCUMENTO,NUMERO_LOTE,TIPO_TRANSACCIONID,ESTADO_ACTIVOID,TIPO_ACTIVOID,COMENTARIO,DESECHADO,MODELO,V_EMPLEADOSIDEMPLEADO,V_ESTACIONID,V_ANFITRIONAID,V_PROVEEDORIDPROVEEDOR,V_MONEDAID,CENTRO_DE_COSTOId,PLACA,ESTADO_PRESTADO")] ACTIVO aCTIVO)
        {
            var original = db.ACTIVOS.Find(aCTIVO.ID);
            if (aCTIVO.DESECHADO)
                return RedirectToAction("Index");


            decimal precio;
            if (db.V_MONEDA.Find(Request["V_MONEDAID"]).NOMBRE.Equals("Colones"))
            {
                // Colones
                decimal tipo_cambio = db.V_TIPO_CAMBIO.ToList()[0].TIPOCAMBIO;
                precio = aCTIVO.PRECIO / tipo_cambio;

            }
            else
            {
                //Dolares
                precio = aCTIVO.PRECIO;

            }
            aCTIVO.TIPO_CAPITAL = (precio >= 1000) ? true : false;

            if (ModelState.IsValid)
            {
                original.NUMERO_SERIE = aCTIVO.NUMERO_SERIE;
                original.FECHA_COMPRA = aCTIVO.FECHA_COMPRA;
                original.INICIO_SERVICIO = aCTIVO.INICIO_SERVICIO;
                original.FABRICANTE = aCTIVO.FABRICANTE;
                original.PRECIO = aCTIVO.PRECIO;
                original.DESCRIPCION = aCTIVO.DESCRIPCION;
                original.EXENTO = aCTIVO.EXENTO;
                original.TIPO_CAPITAL = aCTIVO.TIPO_CAPITAL;
                original.INGRESADO_POR = aCTIVO.INGRESADO_POR;
                original.NUMERO_DOCUMENTO = aCTIVO.NUMERO_DOCUMENTO;
                original.NUMERO_LOTE = aCTIVO.NUMERO_LOTE;
                original.TIPO_TRANSACCIONID = aCTIVO.TIPO_TRANSACCIONID;
                original.TIPO_ACTIVOID = aCTIVO.TIPO_ACTIVOID;
                original.V_ANFITRIONAID = aCTIVO.V_ANFITRIONAID;
                original.V_PROVEEDORIDPROVEEDOR = aCTIVO.V_PROVEEDORIDPROVEEDOR;
                original.DESECHADO = aCTIVO.DESECHADO;
                original.MODELO = aCTIVO.MODELO;
                original.PLACA = aCTIVO.PLACA;
                original.ESTADO_PRESTADO = aCTIVO.ESTADO_PRESTADO;

                db.SaveChanges();

                var consulta_proveedor = db.V_PROVEEDOR.ToList().Where(ea => ea.IDPROVEEDOR == original.V_PROVEEDORIDPROVEEDOR);
                var proveedor = consulta_proveedor.ToList()[0].NOMBRE;
                var consulta_anfitriona = db.V_ANFITRIONA.ToList().Where(ea => ea.ID == original.V_ANFITRIONAID);
                var anfitriona = consulta_anfitriona.ToList()[0].NOMBRE;
                var consulta_transaccion = db.TIPOS_TRANSACCIONES.ToList().Where(ea => ea.ID == original.TIPO_TRANSACCIONID);
                var transaccion = consulta_transaccion.ToList()[0].NOMBRE;

                controladora_transaccion.Create(User.Identity.GetUserName(), "Editado", original.descripcion(proveedor, transaccion, anfitriona), original.ID);

                return RedirectToAction("Index");
            }
            ViewBag.TIPO_TRANSACCIONID = new SelectList(db.TIPOS_TRANSACCIONES.OrderBy(tt => tt.NOMBRE), "ID", "NOMBRE", aCTIVO.TIPO_TRANSACCIONID);
            ViewBag.TIPO_ACTIVOID = new SelectList(db.TIPOS_ACTIVOS.OrderBy(ta => ta.NOMBRE), "ID", "NOMBRE", aCTIVO.TIPO_ACTIVOID);
            ViewBag.V_PROVEEDORIDPROVEEDOR = new SelectList(db.V_PROVEEDOR.OrderBy(p => p.NOMBRE), "IDPROVEEDOR", "NOMBRE", aCTIVO.V_PROVEEDORIDPROVEEDOR);
            ViewBag.V_ANFITRIONAID = new SelectList(db.V_ANFITRIONA.OrderBy(a => a.NOMBRE), "ID", "NOMBRE", aCTIVO.V_ANFITRIONAID);
            ViewBag.V_MONEDAID = new SelectList(db.V_MONEDA, "ID", "SIMBOLO", aCTIVO.V_MONEDAID);
            ViewBag.FECHA_INGRESO = aCTIVO.FECHA_INGRESO.Date;
            return View(aCTIVO);
        }

        // GET: Activos/Delete/5
        [Authorize(Roles = "Desechar activos, superadmin")]
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ACTIVO aCTIVO = db.ACTIVOS.Find(id);
            if (aCTIVO.DESECHADO)
                return RedirectToAction("Index");
            if (aCTIVO == null)
            {
                return HttpNotFound();
            }
            return View(aCTIVO);
        }

        // POST: Activos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Desechar activos, superadmin")]
        public ActionResult DeleteConfirmed(string id)
        {
            ACTIVO aCTIVO = db.ACTIVOS.Find(id);
            if (aCTIVO.DESECHADO)
                return RedirectToAction("Index");

            aCTIVO.DESECHADO = true;
            var estado = db.ESTADOS_ACTIVOS.ToList().Where(ea => ea.NOMBRE == "Desechado");
            aCTIVO.ESTADO_ACTIVOID = estado.ToList()[0].ID;

            var consulta_proveedor = db.V_PROVEEDOR.ToList().Where(ea => ea.IDPROVEEDOR == aCTIVO.V_PROVEEDORIDPROVEEDOR);
            var proveedor = consulta_proveedor.ToList()[0].NOMBRE;
            var consulta_anfitriona = db.V_ANFITRIONA.ToList().Where(ea => ea.ID == aCTIVO.V_ANFITRIONAID);
            var anfitriona = consulta_anfitriona.ToList()[0].NOMBRE;
            var consulta_transaccion = db.TIPOS_TRANSACCIONES.ToList().Where(ea => ea.ID == aCTIVO.TIPO_TRANSACCIONID);
            var transaccion = consulta_transaccion.ToList()[0].NOMBRE;

            db.SaveChanges();
            controladora_transaccion.Create(User.Identity.GetUserName(), "Eliminado", aCTIVO.descripcion(proveedor, transaccion, anfitriona), aCTIVO.ID);
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        /**
         * Metodo que se encarga de actualizar el dropdown de los empleados con base en la estacion seleccionada. Esta hecho para ser llamado
         * por medio de Ajax.
         * @param: El identificador de la estacion
         * @return: Los usuarios que pertenecen a la estacion seleccionada.
         **/
        // Instrucciones de como llenar el List para retornarlo con ajax  --> http://stackoverflow.com/questions/14339089/populating-dropdown-with-json-result-cascading-dropdown-using-mvc3-jquery-aj
        public JsonResult RefrescarUsuarios(string id_estacion)
        {
            var empleados = EmpleadosController.EmpleadosFiltrados(id_estacion);
            return Json(empleados, JsonRequestBehavior.AllowGet);
        }
    }
}
