using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Activos_PrestamosOET.Models;
using System.Data;
using System.Web.UI.WebControls;
using System;
using PagedList;
using System.Globalization;
using System.Collections.Generic;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.html.simpleparser;
using System.Web;
using System.IO;
using System.Net.Http;
using System.Net.Mail;
using Newtonsoft.Json.Linq;
using SendGrid;
using System.Configuration;
using System.Diagnostics;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Objects;
using Microsoft.AspNet.Identity;

namespace Activos_PrestamosOET.Controllers
{

    public class PRESTAMOesController : Controller
    {
        private PrestamosEntities db = new PrestamosEntities();

        protected static int consecutivo;
        protected String generarID()
        {
            consecutivo = (consecutivo + 1) % 999;
            return ""
            + DateTime.Now.Day.ToString("D2")
            + DateTime.Now.Month.ToString("D2")
            + DateTime.Now.Year.ToString()
            + DateTime.Now.Hour.ToString("D2")
            + DateTime.Now.Minute.ToString("D2")
            + DateTime.Now.Second.ToString("D2")
            + DateTime.Now.Millisecond.ToString("D3")
            + consecutivo.ToString("D3");
        }


        //Requiere: el id del prestamo, categoría y número de boleta del préstamo
        //Modifica: Se encarga de recuperar de la tabla de transacciones las observaciones efectuadas para cada activo devuelto de ese préstamo 
        //Regresa: la lista de observaciones de devolución de cada activo de la categoría especificada del préstamo especificado
        public List<String> traerObservaciones(String id, int cat, long? boleta)
        {
            List<String> observaciones = new List<String>();

            //obtiene todos los activos del préstamo
            var activos = db.PRESTAMOS.Include(i => i.ACTIVOes).SingleOrDefault(h => h.ID == id);
            //obtiene el id de todos los activos que pertenecen a la categoría específica
            var act = from a in activos.ACTIVOes.Where(i => i.TIPO_ACTIVOID == cat)
                      select new
                      {
                          ID = a.ID,
                      };

            //Recorre los ids y recupera la observación que se hizo al devolverlos, si es que han sido devueltos
            //de lo contrario no debería encontrar ninguna observación sino que debería obtener la hilera ""
            foreach (var i in act)
            {
                var observacion = from t in db.TRANSACCIONES
                                  where t.ACTIVOID == i.ID && t.ESTADO == "Devuelto de préstamo" && t.NUMERO_BOLETA == boleta
                                  select t.OBSERVACIONES_RECIBO;

                //si consulta observación devuelve nulo es que aún no ha habido transacciones de devolución con ese
                //activo por lo que la hilera deberá ser ""
                if (observacion == null || observacion.Count() == 0)
                {
                    observaciones.Add("");
                }
                else
                {   //En caso de si lograr recuperar algo pasan 2 casos
                    foreach (var o in observacion)
                    {
                        //El caso de que si encuentre una observación por lo que la agregará a lista de observaciones
                        //de esa categoría de activo
                        if (o != null)
                            observaciones.Add(o.ToString());
                        else //en caso de que observacion recupere algo nulo, se agrega esta línea para que devuelva "" tambien
                            observaciones.Add("");
                        //Nota: no se logró averiguar porque a veces observacion no era nulo directamente, sino que obtenía valores nulos por dentro
                        //por eso se agrega "" en ambos casos
                    }
                }
               
            }
            //Finalmente devuelve la lista de observaciones de cada activo de la categoría
            return observaciones;
        }

        //Requiere: vector de booleanos 
        //Modifica: lo convierte en una lista de booleanos que elimina booleanos extra
        //Retorna:  Lista de booleanos
        protected List<bool> corregirVectorBool(bool[] vec)
        {
            List<bool> nuevoVec = new List<bool>();
            int longitud = vec.Count();
            int cuenta = 0;

            while (longitud > cuenta)
            {
                if (vec[cuenta] == true)
                {
                    nuevoVec.Add(true);
                    cuenta += 2;
                }
                else
                {
                    nuevoVec.Add(false);
                    cuenta++;
                }
            }

            return nuevoVec;
        }


        //Requiere: vector de booleanos
        //Modifica: Se fija si hay algún checkbox de categoría checkeado y determina un valor a partir
        //Retorna: booleano 
        protected bool hayFilaEntera(bool[] vec)
        {
            bool ret = false;
            foreach (bool b in vec)
            {
                if (b)
                {
                    ret = true;
                    break;
                }
            }
            return ret;
        }

        //Requiere: String con el id e int de tipo de categoría 
        //Modifica: Hace consulta para recuperar los activos con la categoría y que pertenecen al id
        //Retorna:Lista de listas con información de cada activo en cada lista
        protected List<List<String>> equipoPorCategoria(int cat, String id)
        {
            List<List<String>> equipos = new List<List<String>>();

            var activos = db.PRESTAMOS.Include(i => i.ACTIVOes).SingleOrDefault(h => h.ID == id);
            // int cat = int.Parse(categoria);
            var act = from a in activos.ACTIVOes.Where(i => i.TIPO_ACTIVOID == cat)
                          //where a.ESTADO_PRESTADO == 1
                      select new { FABRICANTE = a.FABRICANTE, MODELO = a.MODELO, PLACA = a.PLACA, ID = a.ID, PRESTADO = a.ESTADO_PRESTADO };

            foreach (var a in act)
            {
                List<String> equipo = new List<String>();
                equipo.Add(a.FABRICANTE);
                equipo.Add(a.MODELO);
                equipo.Add(a.PLACA);
                equipo.Add(a.ID);
                equipo.Add(a.PRESTADO.ToString());
                equipos.Add(equipo);
            }

            return equipos;
        }

        //Requiere: diccionario con lista de listas de hileras con datos de activos
        //Modifica:  Recorre el diccionario e ingresa las placas de todos los activos dentro de él en un lista de hileras
        //Retorna: Devuelve la lista de placas de los activos
        protected List<String> listaActivos(Dictionary<String, List<List<String>>> dic)
        {
            List<String> listaActivos = new List<String>();
            foreach (KeyValuePair<String, List<List<String>>> entrada in dic)
            {
                foreach (List<String> l in entrada.Value)
                {
                    listaActivos.Add(l[2]);
                }
            }
            return listaActivos;
        }

        //Requiere: lista con las placas de los activos del préstamo y placa de un activo
        //Modifica:  Recorre dicha lista y se fija si placa es igual al elemento de la lista para recuperar la posicion de placa dentro de la lista
        //Retorna: Devuelve la posición de placa dentro de lista
        protected int indiceActivo(List<String> lista, String placa)
        {
            int indice = -1;
            for (int i = 0; i < lista.Count; i++)
            {
                if (placa == lista[i])
                {
                    indice = i;
                }
            }
            return indice;
        }

        //Requiere: String con tipo de categoría. 
        //Modifica: Hace consulta para traducir el tipo en un id
        //Retorna:devuelve el id de la categoría como un entero
        protected int traerCategoria(String tipo)
        {
            var consultaCat = from t in db.TIPOS_ACTIVOS
                              where t.NOMBRE.Equals(tipo)
                              select t.ID;

            List<String> categorias = new List<String>();

            foreach (int c in consultaCat)
            {
                categorias.Add(c.ToString());
            }
            int cat = Int32.Parse(categorias[0]);

            return cat;
        }

        // GET: PRESTAMOes
        //Requiere: Recibe 6 parámetros, el primero es la columna por la que se ordenan los datos en la tabla, el segundo, tercero, cuarto y quinto para hacer filtrado de búsqueda y el último para identificar la página en q se encuentra la tabla.
        // Modifica: Maneja el index view, la cual es la vista de consulta de revisión de solicitudes.
        //Retorna: Devuelve una tabla que se despliegará en el index de Revisión de solicitudes.
        [Authorize(Roles = "Aceptar préstamos,superadmin")]
        public ActionResult Index(string sortOrder, string currentFilter, string fechaSolicitud, string fechaRetiro, string estado, string numeroBoleta, int? page)
        {

            string username = User.Identity.GetUserName();

            var users = (from u in db.ActivosUsers select u);

            var user = users.SingleOrDefault(u => u.UserName == username);
            var cedSol = user.Id;

            //prestamos = prestamos.Where(model => model.USUARIO_SOLICITA == cedSol);

            //se identifica si alguna columna fue seleccionada como filtro para ordenar los datos despliegados
            ViewBag.currentSort = sortOrder;
            ViewBag.NumeroSortParm = String.IsNullOrEmpty(sortOrder) ? "numero_dsc" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";
            ViewBag.FDateSortParm = sortOrder == "FDate" ? "FDate_desc" : "FDate";
            ViewBag.PeriodoSortParm = sortOrder == "Periodo" ? "Periodo_desc" : "Periodo";
            ViewBag.NameSortParm = sortOrder == "Name" ? "Name_desc" : "Name";

            if (fechaSolicitud != null || fechaRetiro != null)
            {
                page = 1;
            }
            else
            {
                fechaSolicitud = currentFilter;
            }

            ViewBag.CurrentFilter = fechaSolicitud;
            var prestamos = from p in db.PRESTAMOS select p;//.Include(i => i.ActivosUser);
            prestamos = prestamos.Where(model => model.USUARIO_SOLICITA != cedSol);
            prestamos = prestamos.Include(i => i.ActivosUser);
            //var prestamos = db.PRESTAMOS.Include(i => i.USUARIO);//Se agrega la tabla de usuarios a la de préstamos

            //Inician filtros de búsqueda
            if (!String.IsNullOrEmpty(fechaSolicitud) || !String.IsNullOrEmpty(fechaRetiro))//Caso en que se consulta por una fecha específica
            {
                DateTime fechaS;
                DateTime fechaR;


                if (String.IsNullOrEmpty(fechaSolicitud))//Se ingresó únicamente fecha de inicio del préstamo
                {
                    if (DateTime.TryParseExact(fechaRetiro, "dd/MM/yyyy", new CultureInfo("es"), DateTimeStyles.None, out fechaR))
                    {
                        prestamos = prestamos.Where(model => model.FECHA_RETIRO.Year == fechaR.Year
                                                          && model.FECHA_RETIRO.Month == fechaR.Month
                                                          && model.FECHA_RETIRO.Day == fechaR.Day
                                                          && model.USUARIO_SOLICITA != cedSol);
                    }
                }
                else if (String.IsNullOrEmpty(fechaRetiro))//Se ingresó únicamente la fecha de solicitud del préstamo
                {
                    if (DateTime.TryParseExact(fechaSolicitud, "dd/MM/yyyy", new CultureInfo("es"), DateTimeStyles.None, out fechaS))
                    {
                        prestamos = prestamos.Where(model => model.FECHA_SOLICITUD.Year == fechaS.Year
                                                          && model.FECHA_SOLICITUD.Month == fechaS.Month
                                                          && model.FECHA_SOLICITUD.Day == fechaS.Day
                                                          && model.USUARIO_SOLICITA != cedSol);
                    }
                }
                else//Se ingresaron tanto la fecha de solicitud como de inicio del préstamo.
                {
                    if (DateTime.TryParseExact(fechaSolicitud, "dd/MM/yyyy", new CultureInfo("es"), DateTimeStyles.None, out fechaS))
                    {
                        prestamos = prestamos.Where(model => model.FECHA_SOLICITUD.Year == fechaS.Year
                                                         && model.FECHA_SOLICITUD.Month == fechaS.Month
                                                         && model.FECHA_SOLICITUD.Day == fechaS.Day
                                                         && model.USUARIO_SOLICITA != cedSol);
                    }
                    if (DateTime.TryParseExact(fechaRetiro, "dd/MM/yyyy", new CultureInfo("es"), DateTimeStyles.None, out fechaR))
                    {
                        prestamos = prestamos.Where(model => model.FECHA_RETIRO.Year == fechaR.Year
                                                         && model.FECHA_RETIRO.Month == fechaR.Month
                                                         && model.FECHA_RETIRO.Day == fechaR.Day
                                                         && model.USUARIO_SOLICITA != cedSol);
                    }
                }
            }
            if (!string.IsNullOrEmpty(estado) && estado != "0")//Se consulta por un estado específico. Cero significa todos.
            {
                int est = int.Parse(estado);
                var int16 = Convert.ToInt16(est);
                prestamos = prestamos.Where(model => model.Estado == int16
                    && model.USUARIO_SOLICITA != cedSol);
            }
            if (!string.IsNullOrEmpty(numeroBoleta))
            {
                int num = int.Parse(numeroBoleta);
                prestamos = prestamos.Where(model => model.NUMERO_BOLETA == num
                    && model.USUARIO_SOLICITA != cedSol);
            }
            //Finaliza búsqueda por filtros//

            //Inicio ordenado por columnas//
            switch (sortOrder)//Se ordena la tabla por una columna seleccionada en la vista.
            {
                case "numero_dsc"://Se ordena descendentemente por número de boleta
                    prestamos = prestamos.OrderByDescending(s => s.NUMERO_BOLETA);
                    break;
                case "Date"://Se ordena ascendentemente por fecha de solicitud
                    prestamos = prestamos.OrderBy(s => s.FECHA_SOLICITUD);
                    break;
                case "date_desc"://Se ordena descendentemente por fecha de solicitud
                    prestamos = prestamos.OrderByDescending(s => s.FECHA_SOLICITUD);
                    break;
                case "FDate"://Se ordena ascendentemente por fecha de inicio del préstamo
                    prestamos = prestamos.OrderBy(s => s.FECHA_RETIRO);
                    break;
                case "FDate_desc"://Se ordena descendentemente por fecha de inicio del préstamo
                    prestamos = prestamos.OrderByDescending(s => s.FECHA_RETIRO);
                    break;
                /*case "Periodo":
                    var press = prestamos.ToList().OrderBy(s => s.FECHA_RETIRO.Value.AddDays(s.PERIODO_USO));
                    //prestamos = from pp in prestamos
                    //       orderby (DbFunctions.AddDays(pp.FECHA_RETIRO, pp.PERIODO_USO)) select pp;  
                    // prestamos = prestamos.Cast(press);
                    prestamos = prestamos.OrderBy(s => s.PERIODO_USO).OrderBy(k => k.FECHA_RETIRO.Value.AddDays(k.PERIODO_USO));
                    break;
                case "Periodo_desc":
                    prestamos = prestamos.OrderByDescending(s => s.FECHA_RETIRO.Value.AddDays(s.PERIODO_USO));
                    break;*/
                case "Name"://Ordenado ascendentemento por nombre del solicitante
                    prestamos = prestamos.OrderBy(s => s.USUARIO_SOLICITA);
                    break;
                case "Name_desc"://Ordenado descendentemento por nombre de solicitante
                    prestamos = prestamos.OrderByDescending(s => s.USUARIO_SOLICITA);
                    break;
                default:  // Para todo otro caso se ordena ascendentemente por número de boleta
                    prestamos = prestamos.OrderBy(s => s.NUMERO_BOLETA);
                    break;
            }
            //Finaliza ordenado por columnas//

            //Inicia paginación//
            int pageSize = 5;//Se define número filar por página a desplegar
            int pageNumber = (page ?? 1);//Se define el número de página en que se encuentra
            return View(prestamos.ToPagedList(pageNumber, pageSize));//Se envía la tabla a la paginación
            //Finaliza paginación//                            
        }

        // GET: PRESTAMOes/Historial
        //Requiere: cédula del solicitante, filtro actual de categorías, hilera del estado de la revisión y el identificador de la página en la que se encuentra actualmente.
        //Modifica: Carga la información de la tabla con el historial de solicitudes.
        //Retorna: vista con la tabla en la que se despliega el historial de solicitudes.
        [Authorize(Roles = "Solicitar préstamos,Aceptar préstamos,superadmin")]
        public ActionResult Historial(string CED_SOLICITA, string currentFilter, string estado, int? page)
        {
            //Para que al refrescar la pagina no se quite el filtro por estado
            ViewBag.estado = estado;

            ViewBag.mensajeConfirmacion = (String)TempData["confirmacion"];
            //Consulta todos los prestamos
            var prestamos = from s in db.PRESTAMOS select s;
            //Este es el filtro por cedula del que solicita. Esto deberia ser automatico y filtrar las solicitudes 
            //por el usuario loggeado pero como aun no esta la parte del log in el filtro lo pondremos en 
            //este if por mientras
            if (!string.IsNullOrEmpty(CED_SOLICITA))
            {
                //En el historial solo deben aparecer los prestamos de la persona que esta loggeada
                prestamos = prestamos.Where(model => model.USUARIO_SOLICITA == CED_SOLICITA);
            }

            string username = User.Identity.GetUserName();

            var users = (from u in db.ActivosUsers select u);

            var user = users.SingleOrDefault(u => u.UserName == username);
            var cedSol = user.Id;

            prestamos = prestamos.Where(model => model.USUARIO_SOLICITA == cedSol);

            //Verfica el filtro de estado. Si el usuario no selecciono ningun filtro, entonces no se filtra por estado
            //pero si si selecciono el estado por el que quiere filtrar entonces, filtra por eso
            int est;
            if (string.IsNullOrEmpty(estado))
            {
                est = 0;
            }
            else
            {
                est = int.Parse(estado);
            }
            if (!string.IsNullOrEmpty(estado) && estado != "0")
            {
                var int16 = Convert.ToInt16(est);
                prestamos = prestamos.Where(model => model.Estado == int16);
            }
            //En el historial, las solicitudes siempre estan ordenadas por la fecha de solicitud (de la mas reciente a la mas vieja)
            prestamos = prestamos.OrderByDescending(s => s.FECHA_SOLICITUD);
            //para la paginacion de la tabla
            int pageSize = 5;
            int pageNumber = (page ?? 1);
            return View(prestamos.ToPagedList(pageNumber, pageSize));
        }

        public string viewBagFechaSolicitada(DateTime sol)
        {
            string f = sol.Date.ToShortDateString();
            f = f.Replace("/20", "/");
            return f;
        }

        // GET: PRESTAMOes/Detalles
        //Requiere: id del Préstamo
        //Modifica: Recupera la información sobre la solicitud de Préstamo seleccionads y la muestra
        //Retorna: Vista con la información de los detalles de un Préstamo específico.
        [Authorize(Roles = "Solicitar préstamos,Aceptar préstamos,superadmin")]
        public ActionResult Detalles(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
           
            PRESTAMO pRESTAMO = db.PRESTAMOS.Find(id);
            // ViewBag.clear();

            if (pRESTAMO == null)
            {
                return HttpNotFound();
            }
            string username = User.Identity.GetUserName();
            var user = db.ActivosUsers.SingleOrDefault(u => u.UserName == username);
            if (user.Id != pRESTAMO.USUARIO_SOLICITA)
                return RedirectToAction("Historial");
            ViewBag.Estadillo = "";
            if (pRESTAMO.Estado == 1)
            {
                ViewBag.Estadillo = "Pendiente";
            }
            else if (pRESTAMO.Estado == 2)
            {
                ViewBag.Estadillo = "Aceptada";
            }
            else if (pRESTAMO.Estado == 3)
            {
                ViewBag.Estadillo = "Denegada";
            }
            else if (pRESTAMO.Estado == 4)
            {
                ViewBag.Estadillo = "Abierta";
            }
            else if (pRESTAMO.Estado == 5)
            {
                ViewBag.Estadillo = "Cerrada";
            }
            else if (pRESTAMO.Estado == 6)
            {
                ViewBag.Estadillo = "Cancelada";
            }

            var cursos = (from u in db.V_COURSES select u);
            V_COURSES curso = cursos.SingleOrDefault(u => u.COURSES_CODE == pRESTAMO.SIGLA_CURSO);
            try
            {
                ViewBag.MiCurso = curso.COURSE_NAME;
            } catch
            {
                if (pRESTAMO.SIGLA_CURSO == null)
                {
                    ViewBag.MiCurso = "";
                }
                else
                {
                    ViewBag.MiCurso = pRESTAMO.SIGLA_CURSO;
                }
            }
            var lista = from o in db.PRESTAMOS
                        from o2 in db.ActivosUsers
                        where o.ID == id
                        select new { Prestamo = o, CEDULA = o2.Cedula, USUARIO = o2.Nombre };

            foreach (var m in lista)
            {
                if (m.Prestamo.ID == id)
                {
                    if (m.Prestamo.USUARIO_SOLICITA == m.CEDULA)
                    {
                        var t = new Tuple<string>(m.USUARIO);
                        ViewBag.Nombre = t.Item1;
                    }
                }
            }

            var cat = (from ac in db.ACTIVOS
                       from t in db.TIPOS_ACTIVOS
                       where ac.PRESTABLE.Equals(true) &&
                              t.ID.Equals(ac.TIPO_ACTIVOID)
                       select new { t.NOMBRE, t.ID }).Distinct();

            var equipo_sol = from o in db.PRESTAMOS
                             from o2 in db.EQUIPO_SOLICITADO
                             where (o.ID == id && o2.ID_PRESTAMO == id)
                             select new { ID = o.ID, ID_EQUIPO = o2.ID_PRESTAMO, TIPO = o2.TIPO_ACTIVO, CANTIDAD = o2.CANTIDAD, CANTAP = o2.CANTIDADAPROBADA };

            var equipo = new List<List<String>>();

            foreach (var y in cat)
            {
                bool existeCategoria = false;
                List<String> temp = new List<String>();
                foreach (var x in equipo_sol)
                {

                    if (x.TIPO != null)
                    {
                        if (x.TIPO == y.NOMBRE)
                        {

                            temp.Add(y.NOMBRE);
                            existeCategoria = true;
                            if (x.CANTIDAD != 0) { temp.Add(x.CANTIDAD.ToString()); } else { temp.Add("0"); }
                            if (x.CANTAP != 0) { temp.Add(x.CANTAP.ToString()); } else { temp.Add("0"); }
                            break;
                        }
                    }
                    else
                    {
                        temp.Add("");
                        temp.Add("");
                        temp.Add("");
                    }
                }
                if (!existeCategoria)
                {
                    temp.Add(y.NOMBRE);
                    temp.Add("0");
                    temp.Add("0");

                }
                equipo.Add(temp);
            }
            ViewBag.Equipo_Solict = equipo;
            return View(pRESTAMO);
        }

        // GET: PRESTAMOes/Details/5
        //Requiere: Recibe el id del prestamo que se está consultando.
        // Modifica: Maneja el details view, la cual es la vista de consulta de revisión de una solicitud en particular.
        //Retorna: Devuelve un información necesaria para el despliegue de la vista como: nombre de solicitante, el estado, el equipo solicitado y sus cantidades
        [Authorize(Roles = "Aceptar préstamos,superadmin")]
        public ActionResult Details(string id)
        {
            //Mensajes de alerta, de exito, etc.
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (TempData["Mensaje"] != null)
            {
                ViewBag.Mensaje = TempData["Mensaje"].ToString();
                TempData.Remove("Mensaje");
            }
            if (TempData["Mensaje2"] != null)
            {
                ViewBag.Mensaje2 = TempData["Mensaje2"].ToString();
                TempData.Remove("Mensaje2");
            }

            PRESTAMO pRESTAMO = db.PRESTAMOS.Find(id);

            if (pRESTAMO == null)
            {
                return HttpNotFound();
            }
            //Se encuentra el nombre del solicitante
            var lista = from o in db.PRESTAMOS
                        from o2 in db.ActivosUsers
                        where o.ID == id
                        select new { Prestamo = o, CEDULA = o2.Cedula, USUARIO = o2.Nombre };

            foreach (var m in lista)
            {
                if (m.Prestamo.ID == id)
                {
                    if (m.Prestamo.USUARIO_SOLICITA == m.CEDULA)
                    {
                        var t = new Tuple<string>(m.USUARIO);
                        ViewBag.Nombre = t.Item1;
                    }
                }
            }
            /*  -------------------------------------------------------------------------------------------  */

            //Se maneja el llenar la tabla con las cantidades solicitadas
            var equipoSol = db.PRESTAMOS.Include(i => i.EQUIPO_SOLICITADO).SingleOrDefault(p => p.ID == id);
            var equipoSolicitado = equipoSol.EQUIPO_SOLICITADO;

            var equipo = new List<List<String>>();
            var actPrevios = new List<List<String>>();
            var act = new List<List<String>>();
            var activos = new List<List<List<String>>>();
            var activosPrevios = new List<List<List<String>>>();
            foreach (var x in equipoSolicitado)
            {
                List<String> temp = new List<String>();
                if (x.TIPO_ACTIVO != null)
                {
                    temp.Add(x.TIPO_ACTIVO.ToString());
                    //Se maneja llenar la tabla del modal
                    actPrevios = llenarTablaDetails(x.TIPOS_ACTIVOSID.ToString(), id);
                    act = llenarTablaDetails(x.TIPOS_ACTIVOSID.ToString());
                }
                else
                {
                    temp.Add("");
                }
                if (x.CANTIDAD != 0)
                {
                    temp.Add(x.CANTIDAD.ToString());
                }
                else
                {
                    temp.Add("");
                }
                if (x.CANTIDADAPROBADA != 0)
                {
                    temp.Add(x.CANTIDADAPROBADA.ToString());
                }
                else
                {
                    temp.Add("");
                }
                equipo.Add(temp);
                activosPrevios.Add(actPrevios);
                activos.Add(act);
            }
            
            ViewBag.Activos_enPrevio = activosPrevios;
            ViewBag.Activos_enCat = activos;
            //Segmento de código para colocar colores a las cantidad de solicitudes por categoría.
            var prestamosConEquipo = db.PRESTAMOS.Include(j => j.EQUIPO_SOLICITADO).SingleOrDefault(p => p.ID == id);//Se hace joint entre prestamos y equipo solicitado por id del préstamo           
            var equipoMayorCero = prestamosConEquipo.EQUIPO_SOLICITADO.Where(q => q.CANTIDAD > 0);//Se verifica que se seleccionen los equipos seleccionados que tengan más 0 soliciudes
            var prestamosPorFechas = db.PRESTAMOS.Include(j => j.EQUIPO_SOLICITADO).Where(p => p.FECHA_RETIRO <= prestamosConEquipo.FECHA_RETIRO && p.ID != id);//Se selccionan las solicitudes de préstamo qe se encuentran "abiertos" para el momento de inicio del préstamo consultado.
            Dictionary<string, int> hashConValoresPorTipoActivo = new Dictionary<string, int>();//diccionario que almacena las cantidades de préstamos vigentes.
            if (prestamosPorFechas.ToList() != null)
            {
                foreach (var f in prestamosPorFechas)
                {
                    if (f.FECHA_RETIRO.AddDays(f.PERIODO_USO) >= prestamosConEquipo.FECHA_RETIRO)
                    {
                        var equipoFechasMayorCero = f.EQUIPO_SOLICITADO.Where(q => q.CANTIDAD > 0);//Se seleccionan pedidos con una cantidad mayor a 0
                        if (equipoFechasMayorCero.ToList() != null)
                        {
                            foreach (var e in equipoFechasMayorCero)
                            {
                                foreach (var pp in equipoMayorCero)
                                {
                                    if (e.TIPO_ACTIVO == pp.TIPO_ACTIVO)//Si los prestamos "abiertos" tienen un articulo solicitado en el prestamo consultado, se registra la categoría y la cantidad
                                    {//Se guarda en el diccionario
                                        if (hashConValoresPorTipoActivo.ContainsKey(pp.TIPOS_ACTIVOSID.ToString()))
                                        {
                                            int value = Convert.ToInt32(hashConValoresPorTipoActivo[pp.TIPOS_ACTIVOSID.ToString()]);
                                            value += Convert.ToInt32(e.CANTIDAD);
                                            hashConValoresPorTipoActivo[pp.TIPOS_ACTIVOSID.ToString()] = value;
                                        }
                                        else
                                        {
                                            hashConValoresPorTipoActivo.Add(pp.TIPOS_ACTIVOSID.ToString(), int.Parse(e.CANTIDAD.ToString()));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            List<string> disp = new List<string>();
            foreach (var e in equipoMayorCero)
            {

                int tipo = e.TIPOS_ACTIVOSID;
                //Se consulta la cantidad total de activos prestables de una categoría 
                int contador = (from a in db.ACTIVOS
                                where a.TIPO_ACTIVOID == tipo
                                select a).Count();
                int total = 0;
                if (hashConValoresPorTipoActivo.ContainsKey(e.TIPOS_ACTIVOSID.ToString()))
                {

                    total = Convert.ToInt32(e.CANTIDAD) + hashConValoresPorTipoActivo[e.TIPOS_ACTIVOSID.ToString()];//
                }
                else
                {
                    total = Convert.ToInt32(e.CANTIDAD);
                }
                if (total <= contador)//Si el total de activos solicitados para un periodo específico es meonr que el total de activos prestables, se retorna un disponible ("d")
                {
                    disp.Add("d");
                }
                else//Caso contrario, se retorna un indisponible ("i")
                {
                    disp.Add("i");
                }
            }
            int k = 0;
            foreach (var l in equipo)//Se agrega el resultado del cálculo para cada categoría al final del vector a retornar.
            {
                try
                {
                    l.Add(disp[k]);
                    k++;
                }
                catch (ArgumentOutOfRangeException e)
                {
                    l.Add("d");

                }
            }
            string username = User.Identity.GetUserName();
            ViewBag.Equipo_Solict = equipo;
            var users = from u in db.ActivosUsers
                        where u.UserName == username
                        select u;

            //select u.Cedula); 
            var user = users.SingleOrDefault(u => u.UserName == username);
            //User cedSol = user.Id;
            //;
            try
            {
                if (user.Id == pRESTAMO.USUARIO_SOLICITA)
                {
                    ViewBag.mismo = true;
                }
                else
                {
                    ViewBag.mismo = false;
                }
            }catch
            {
                ViewBag.mismo = false;
            }

            return View(pRESTAMO);
        }


        //Requiere: Recibe el id del prestamo que se está consultando, un vector desde la vista que envia las cantidades de equipo solicitado y un string q se usa para determinar que boton fue apretado.
        // Modifica: Maneja el details view, la cual es la vista de consulta de revisión de una solicitud en particular.
        //Retorna: Devuelve un información necesaria para el despliegue de la vista como: nombre de solicitante, el estado, el equipo solicitado y sus cantidades, además, despliega un mensaje de confirmacion diferente de acuerdo a si el boton fue aceptar o denegar

        [HttpPost]
        [Authorize(Roles = "Aceptar préstamos,superadmin")]
        public ActionResult Details(string ID, int[] cantidad_aprobada, string[] activoSeleccionado, string b, [Bind(Include = "ID,NUMERO_BOLETA,MOTIVO,FECHA_SOLICITUD,FECHA_RETIRO,PERIODO_USO,SOFTWARE_REQUERIDO,OBSERVACIONES_SOLICITANTE,OBSERVACIONES_APROBADO,OBSERVACIONES_RECIBIDO,CEDULA_USUARIO,SIGLA_CURSO")] PRESTAMO p)
        {
            //Se guarda las observaciones de aprobacion
            PRESTAMO pRESTAMO = db.PRESTAMOS.Find(ID);
            pRESTAMO.OBSERVACIONES_APROBADO = p.OBSERVACIONES_APROBADO;

            //agregar quien aprueba el prestamo

            string username = User.Identity.GetUserName();
            var users = (from u in db.ActivosUsers select u);
            var user = users.SingleOrDefault(u => u.UserName == username);
            var cedSol = user.Id;
            pRESTAMO.USUARIO_APRUEBA = cedSol.ToString();

            if (ModelState.IsValid)
            {
                db.Entry(pRESTAMO).State = EntityState.Modified;
                db.SaveChanges();
            }

            var prestamo = db.PRESTAMOS.Include(i => i.EQUIPO_SOLICITADO).SingleOrDefault(h => h.ID == ID);



            var equipo_sol = prestamo.EQUIPO_SOLICITADO;
            //Si el boton de aceptar fue precionado
            if (b == "Aceptar")
            {
                int a = 0;
                //Se almacena la cantidad aprobada por cada categoria
                foreach (var x in equipo_sol)
                {
                    if (prestamo.ID == x.ID_PRESTAMO)
                    {

                        EQUIPO_SOLICITADO P = db.EQUIPO_SOLICITADO.Find(ID, x.TIPO_ACTIVO, x.CANTIDAD);

                        decimal temp = cantidad_aprobada[a];

                        P.CANTIDADAPROBADA = temp;

                        if (ModelState.IsValid)
                        {
                            db.Entry(P).State = EntityState.Modified;
                            db.SaveChanges();
                        }

                        a++;
                    }
                }
                //Se almacena los activos que han sido asignados al prestamo
                if (pRESTAMO.Estado == 1)
                {
                    pRESTAMO.Estado = 2;
                    if (ModelState.IsValid)
                    {
                        db.Entry(pRESTAMO).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
                addActivosToPrestamo(activoSeleccionado, ID);
                ViewBag.Mensaje = "El préstamo ha sido aprobado con éxito";
                TempData["Mensaje"] = "El préstamo ha sido aprobado con éxito";

            }


            //Si se presiona el boton de denegar
            if (b == "Denegar")
            {
                //Se cambia el estado

                pRESTAMO.Estado = 3;
                if (ModelState.IsValid)
                {
                    db.Entry(pRESTAMO).State = EntityState.Modified;
                    db.SaveChanges();
                }

                ViewBag.Mensaje2 = "El préstamo ha sido denegado con éxito";
                TempData["Mensaje2"] = "El préstamo ha sido denegado con éxito";
            }

            //Si se presiona el boton de descargar la boleta
            if (b == "Descargar Boleta")
            {
                DownloadPDF("BoletaPDF", pRESTAMO, "BoletaSoliciud");
            }

            return RedirectToAction("Details", new { id = ID });
        }

        //Requiere: Recibe el mensaje que se quiere enviar por correo electronico.
        // Modifica: Envia un correo electronico.
        //Retorna: N/A
        private static void SendAsync(SendGrid.SendGridMessage message)
        {

            //string apikey = Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
            // Create a Web transport for sending email.
            var credentials = new NetworkCredential(
                       ConfigurationManager.AppSettings["mailAccount"],
                       ConfigurationManager.AppSettings["mailPassword"]
                       );
            var transportWeb = new SendGrid.Web(credentials);

            //Enviar el email
            transportWeb.DeliverAsync(message);
        }

        //Requiere: Recibe 3 strings, el primero es la direccion electronica a la que se quiere enviar el email, la segunda indica el mensaje a enviar y la tercera es el asunto.
        // Modifica: Envia un correo electronico.
        //Retorna: N/A
        private static void SolicitudBien(string to, string mensaje, string subj)
        {
            // Create the email object first, then add the properties.
            var myMessage = new SendGrid.SendGridMessage();
            myMessage.AddTo(to);
            myMessage.From = new System.Net.Mail.MailAddress(
                                "message.ots@tropicalstudies.org", "Admin");
            myMessage.Subject = subj;
            myMessage.Text = mensaje;
            myMessage.Html = mensaje;


            //Enviar el email
            SendAsync(myMessage);
        }

        //Requiere: un int que corresponde al tipo de notificacion que se debe enviar y un string que corresponde al id del prestamo.
        // Modifica: Envia un correo electronico al encargado dependiendo del tipo de notificacion que se quiera enviar.
        //Retorna: N/A
        private void emailEncargado(string idd, int tipo)
        {
            //Busca el prestamo del que hay que enviar los detalles
            PRESTAMO p = db.PRESTAMOS.Find(idd);

            //Para enviar los detalles de la solicitud por correo eletronico
            //Obtiene la vista con los datos y la convierte en string
            string HTMLContent = RenderRazorViewToString("DetallesPDF", p);

            //Para enviar el link a los detalles de la solicitud
            var consultaUrl = Url.Action("Details", "PRESTAMOes", new { id = idd }, protocol: Request.Url.Scheme);
            string link = " " + consultaUrl + " ";
            string subj = "";
            string mensajito = "";

            //Redaccion del email dependiendo de que tipo de notificacion sea
            switch (tipo)
            {
                case 1://Si es una solicitud nueva
                    subj = "Nueva Solicitud de Prestamo: " + p.NUMERO_BOLETA.ToString();
                    mensajito = "Se ha realizado una solicitud de prestamo." + " \n " + "El numero de boleta es " + p.NUMERO_BOLETA.ToString() + ". \n " + " Puede consultar la solicitud en el siguiente link:" + link;
                    mensajito = mensajito + HTMLContent;
                    break;

                case 2://Si se ha editado una solicitud
                    subj = "Edición de Prestamo: " + p.NUMERO_BOLETA.ToString();
                    mensajito = "Se ha editado una solicitud." + " \n " + "El numero de boleta es " + p.NUMERO_BOLETA.ToString() + ". \n " + " Puede consultar la solicitud en el siguiente link:" + link;
                    mensajito = mensajito + "\n" + HTMLContent;
                    break;

                case 3://Si se cancela una solicitud
                    subj = "Cancelación de Prestamo: " + p.NUMERO_BOLETA.ToString();
                    mensajito = "Se ha cancelado una solicitud." + " \n " + "El numero de boleta es " + p.NUMERO_BOLETA.ToString() + ". \n " + " Puede consultar la solicitud en el siguiente link:" + link;
                    mensajito = mensajito + HTMLContent;
                    break;
            }

            //Email indicado al que hay que enviar los emails electronicos para notificaciones con respecto a prestamos
            string email = "tiquetes.soporte@tropicalstudies.org";//User.Identity.Name;

            //Envia la solicitud dependiendo del email, mensaje y asunto determinados en este metodo
            SolicitudBien(email, mensajito, subj);
        }

        //Requiere: un int que corresponde al tipo de notificacion que se debe enviar y un string que corresponde al id del prestamo.
        // Modifica: Envia un correo electronico al cliente que esta loggeado en este momento dependiendo del tipo de notificacion que se quiera enviar.
        //Retorna: N/A
        private void emailCliente(string idd, int tipo)
        {
            //Busca el prestamo del que hay que enviar los detalles
            PRESTAMO p = db.PRESTAMOS.Find(idd);

            //Para enviar los detalles de la solicitud por correo eletronico
            //Obtiene la vista con los datos y la convierte en string
            string HTMLContent = RenderRazorViewToString("DetallesPDF", p);

            //Para enviar el link a los detalles de la solicitud
            var consultaUrl = Url.Action("Detalles", "PRESTAMOes", new { id = idd }, protocol: Request.Url.Scheme);
            string link = " " + consultaUrl + " ";
            string subj = "";
            string mensajito = "";

            //Redaccion del email dependiendo de que tipo de notificacion sea
            switch (tipo)
            {
                case 1://Si es una solicitud nueva
                    subj = "Nueva Solicitud de Prestamo: " + p.NUMERO_BOLETA.ToString();
                    mensajito = "Su solicitud ha sido realizada con éxito." + " \n " + "El numero de boleta es " + p.NUMERO_BOLETA.ToString() + ". \n " + " Puede consultar la solicitud en el siguiente link:" + link;
                    mensajito = mensajito + HTMLContent;
                    break;

                case 2://Si se ha editado una solicitud
                    subj = "Edición de Prestamo: " + p.NUMERO_BOLETA.ToString();
                    mensajito = "Su prestamo ha sido editado exitosamente." + " \n " + "El numero de boleta es " + p.NUMERO_BOLETA.ToString() + ". \n " + " Puede consultar la solicitud en el siguiente link:" + link;
                    mensajito = mensajito + "\n" + HTMLContent;
                    break;

                case 3://Si se cancela una solicitud
                    subj = "Cancelación de Prestamo: " + p.NUMERO_BOLETA.ToString();
                    mensajito = "Su prestamo se ha cancelado exitosamente." + " \n " + "El numero de boleta es " + p.NUMERO_BOLETA.ToString() + ". \n " + " Puede consultar la solicitud en el siguiente link:" + link;
                    mensajito = mensajito + HTMLContent;
                    break;
            }

            //El email al que hay que enviar la solicitud, es al del cliente que la acaba de crear que debe estar loggeado en el sistema.
            string email = User.Identity.Name;

            //Envia la solicitud dependiendo del email, mensaje y asunto determinados en este metodo
            SolicitudBien(email, mensajito, subj);
        }

        // GET: PRESTAMOes/Create
        //Requiere: N/A.
        // Modifica: Crea la vista del Create de prestamo.
        //Retorna: una vista
        [Authorize(Roles = "Solicitar préstamos,Aceptar préstamos,superadmin")]
        public ActionResult Create()
        {

            ViewBag.SIGLA_CURSO = new SelectList(db.V_COURSES, "COURSES_CODE", "COURSE_NAME");
            ViewBag.MensajeError = (String)TempData["error"];

            List<String> categorias = new List<String>();

            var cat = (from ac in db.ACTIVOS
                       from t in db.TIPOS_ACTIVOS
                       where ac.PRESTABLE.Equals(true) &&
                              t.ID.Equals(ac.TIPO_ACTIVOID)
                       select t.NOMBRE).Distinct();

            foreach (String c in cat)
            {
                categorias.Add(c.ToString());
            }

            ViewData["categorias"] = categorias;
            TempData["categorias"] = categorias;
            TempData.Keep();

            return View();
        }

        
        //Requiere: PRESTAMO p, int[] Cantidad, String[] Categoria.
        // Modifica: Inserta en la base de datos el p ingresado como parametro, envia una notificacion por medio de email y redirecciona al historial.
        //Retorna: una vista
        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Solicitar préstamos,superadmin")]
        public ActionResult Create([Bind(Include = "ID,NUMERO_BOLETA,MOTIVO,FECHA_SOLICITUD,FECHA_RETIRO,PERIODO_USO,SOFTWARE_REQUERIDO,OBSERVACIONES_SOLICITANTE,OBSERVACIONES_APROBADO,OBSERVACIONES_RECIBIDO,SIGLA_CURSO,Estado,USUARIO_SOLICITA,USUARIO_APRUEBA")] PRESTAMO p, int[] Cantidad, String[] Categoria, bool asignadoACurso, String Fecha_Inicio_Curso)
        {

            bool sinActivos = true;

            for (int i = 0; i < Cantidad.Count(); i++)
            {
                if (Cantidad[i] != 0)
                {
                    sinActivos = false;
                    break;
                }
            }

            if (sinActivos)
            {
                TempData["error"] = "Debe solicitar al menos un activo";
                return RedirectToAction("Create");
            }

            //Metemos los valores ingresados por el usuario en un nuevo prestamo
            PRESTAMO prestamo = new PRESTAMO();
            var allErrors = ModelState.Values.SelectMany(v => v.Errors);
            if (Cantidad == null)
            {
                return RedirectToAction("Create");
            }
            DateTime fecha;
            if (asignadoACurso)
            {
                ModelState["FECHA_RETIRO"].Errors.Clear();
                fecha = DateTime.ParseExact(Fecha_Inicio_Curso, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            else
            {
                fecha = p.FECHA_RETIRO;
            }

            if (ModelState.IsValid)
            {
                string idd = generarID();
                string username = User.Identity.GetUserName();

                var users = from u in db.ActivosUsers
                            where u.UserName == username
                            select u;

                var user = users.SingleOrDefault(u => u.UserName == username);
                var cedSol = user.Id;
                prestamo.ID = idd;
                prestamo.MOTIVO = p.MOTIVO;
                prestamo.NUMERO_BOLETA = 1;
                prestamo.OBSERVACIONES_APROBADO = "";
                prestamo.OBSERVACIONES_RECIBIDO = "";
                prestamo.OBSERVACIONES_SOLICITANTE = p.OBSERVACIONES_SOLICITANTE;
                prestamo.PERIODO_USO = p.PERIODO_USO;
                prestamo.SIGLA_CURSO = p.SIGLA_CURSO;
                prestamo.USUARIO_APRUEBA = p.USUARIO_APRUEBA;
                prestamo.USUARIO_SOLICITA = cedSol.ToString();
                prestamo.FECHA_RETIRO = fecha;
                prestamo.FECHA_SOLICITUD = System.DateTimeOffset.Now.Date;
                prestamo.SOFTWARE_REQUERIDO = p.SOFTWARE_REQUERIDO;
                prestamo.Estado = 1;
                if (p.SIGLA_CURSO != null)
                {
                    var course = db.V_COURSES.SingleOrDefault(c => c.COURSES_CODE == p.SIGLA_CURSO);
                    var idCourse = course.COURSES;
                    prestamo.V_COURSESCOURSES = idCourse;
                }
                db.PRESTAMOS.Add(prestamo);

                //Guardamos el prestamo en la base
                db.SaveChanges();
                List<String> cat = (List<String>)TempData["categorias"];

                //Ingresamos el equipo solicitado y hacemos la insercion en la base de datos
                for (int i = 0; i < Cantidad.Length; i++)
                {
                    EQUIPO_SOLICITADO equipo = new EQUIPO_SOLICITADO();
                    if (Cantidad[i] == 0)
                    {
                        continue;
                    }
                    else
                    {
                        equipo.CANTIDAD = Cantidad[i];
                    }
                    equipo.TIPO_ACTIVO = cat[i];
                    equipo.TIPOS_ACTIVOSID = traerCategoria(cat[i]);
                    equipo.ID_PRESTAMO = prestamo.ID;
                    db.EQUIPO_SOLICITADO.Add(equipo);
                    db.SaveChanges();
                }

                //Buscamos el prestamo recien insertado en la base de datos
                //Esto es necesario porque el numero de boleta se ingresa hasta que se crea la solicitud en base 
                PRESTAMO prest = new PRESTAMO();
                prest = db.PRESTAMOS.Find(idd);

                //Refresca el contexto del objeto PRESTAMO prest de la base de datos para obtener el numero de solicitud correcto.
                var ctx = ((IObjectContextAdapter)db).ObjectContext;
                ctx.Refresh(RefreshMode.ClientWins, prest);

                //Envia los correos de notificacion al cliente y al encargado
                emailCliente(idd, 1);
                //emailEncargado(idd, 1);

                //Mensaje de confirmacion
                TempData["confirmacion"] = "La solicitud fue enviada con éxito";
                TempData.Keep();
                return RedirectToAction("Historial");
            }

            ViewBag.SIGLA_CURSO = new SelectList(db.V_COURSES, "COURSES_CODE", "COURSE_NAME");
            return View(prestamo);

        }


        // GET: PRESTAMOes/Edit/5
        //Requiere: identificador del Préstamo.
        //Modifica: Carga los campos en los que se pueden cambiar datos para editar información relacionada a un préstamo específico.
        //Retorna: vista con los campos para editar solicitud.
        [Authorize(Roles = "Solicitar préstamos,Aceptar préstamos,superadmin")]
        public ActionResult Edit(string id)
        {
            //Si el id es null da error
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            //Busca la solicitud que se quiere editar
            PRESTAMO pRESTAMO = db.PRESTAMOS.Find(id);

            //si no esta devuelve error
            if (pRESTAMO == null)
            {
                return HttpNotFound();
            }

            //Buscamos los cursos de en la base de datos
            //ViewBag.Cursos = new SelectList(db.V_COURSES, "COURSES_CODE", "COURSE_NAME");
            //String SelectCurso = "<select class=\"form-control\" id=\"SIGLA_CURSO\" name=\"SIGLA_CURSO\">";
            /* if (pRESTAMO.SIGLA_CURSO != null)
             {*/
            /*
           SelectList Cursos = new SelectList(db.V_COURSES.ToList());//new SelectList(db.V_COURSES, "COURSES_CODE", "COURSE_NAME");
           bool seleccionadoNull = false;

           foreach (V_COURSES curso in db.V_COURSES.ToList())
           {
               if (pRESTAMO.SIGLA_CURSO == curso.COURSES_CODE)
               {
                   SelectCurso += "<option value=\"" + curso.COURSES_CODE + "\" selected=\"selected\">" + curso.COURSE_NAME + "</option>";
                   seleccionadoNull = true;
               }
               else
               {
                   SelectCurso += "<option value=\"" + curso.COURSES_CODE + "\">" + curso.COURSE_NAME + "</option>";
               }
           }
           if (!seleccionadoNull)
           {
               SelectCurso += "<option value=\"\">Seleccione</option>";
           }
           else
           {
               SelectCurso += "<option value=\"" + "\" selected=\"selected\">Seleccione</option>";
           }
           SelectCurso += "</select>";
           ViewBag.SelectCurso = SelectCurso;
           */
            
            /*
            if (pRESTAMO.V_COURSESCOURSES != 0)
            {
                var course = db.V_COURSES.SingleOrDefault(c => c.COURSES_CODE == pRESTAMO.SIGLA_CURSO);
                idCourse = course.COURSES;
                //prestamo.V_COURSESCOURSES = idCourse;
            }*/
            bool b = false;
            String SelectCurso = "<select class=\"form-control\" id=\"SIGLA_CURSO\" name=\"SIGLA_CURSO\">";
            foreach (V_COURSES curso in db.V_COURSES.ToList())
            {
                if (pRESTAMO.V_COURSESCOURSES == curso.COURSES)
                {
                    SelectCurso += "<option value=\"" + curso.COURSES_CODE + "\" selected=\"selected\">" + curso.COURSE_NAME + "</option>";
                    b = true;
                }
                else
                {
                    SelectCurso += "<option value=\"" + curso.COURSES_CODE + "\">" + curso.COURSE_NAME + "</option>";
                }
            }
            if(b==true)
            {
                SelectCurso= SelectCurso + "<option value=\"\">Seleccione</option>";
            }else
            {
                SelectCurso += "<option value=null selected=\"selected\">Seleccione</option>";
                //SelectCurso += "<option value=\"" + "\" selected=\"selected\">Seleccione</option>";
            }
            SelectCurso += "</select>";

            ViewBag.SelectCurso = SelectCurso;
            //SelectList cursosDDL = new SelectList(db.V_COURSES);

            //Determina el estado de la solicitud para desplegarlo en la pantalla mas adelante
            ViewBag.Estadillo = "";
            if (pRESTAMO.Estado == 1)
            {
                ViewBag.Estadillo = "Pendiente";
            }
            else if (pRESTAMO.Estado == 2)
            {
                ViewBag.Estadillo = "Aceptada";
            }
            else if (pRESTAMO.Estado == 3)
            {
                ViewBag.Estadillo = "Denegada";
            }
            else if (pRESTAMO.Estado == 4)
            {
                ViewBag.Estadillo = "Abierta";
            }
            else if (pRESTAMO.Estado == 5)
            {
                ViewBag.Estadillo = "Cerrada";
            }
            else if (pRESTAMO.Estado == 6)
            {
                ViewBag.Estadillo = "Cancelada";
            }

            ViewBag.fechSol = viewBagFechaSolicitada(pRESTAMO.FECHA_SOLICITUD.Date);

            //Consulta el usuario solicitante
            var lista = from o in db.PRESTAMOS
                        from o2 in db.ActivosUsers
                        where o.ID == id
                        select new { Prestamo = o, CEDULA = o2.Cedula, USUARIO = o2.Nombre };

            //busca el nombre del usuario solicitante
            foreach (var m in lista)
            {
                if (m.Prestamo.ID == id)
                {
                    if (m.Prestamo.USUARIO_SOLICITA == m.CEDULA)
                    {
                        var t = new Tuple<string>(m.USUARIO);
                        ViewBag.Nombre = t.Item1;
                    }
                }
            }

            //Busca las categorias existentes
            var cat = (from ac in db.ACTIVOS
                       from t in db.TIPOS_ACTIVOS
                       where ac.PRESTABLE.Equals(true) &&
                              t.ID.Equals(ac.TIPO_ACTIVOID)
                       select new { t.NOMBRE, t.ID }).Distinct();

            //busca al equipo previamente solicitado 
            var equipo_sol = from o in db.PRESTAMOS
                             from o2 in db.EQUIPO_SOLICITADO
                             where (o.ID == id && o2.ID_PRESTAMO == id)
                             select new { ID = o.ID, ID_EQUIPO = o2.ID_PRESTAMO, TIPO = o2.TIPO_ACTIVO, CANTIDAD = o2.CANTIDAD, CANTAP = o2.CANTIDADAPROBADA };

            //Acomoda la informacion de la tabla de equipo solicitado que se desplegara en la pantalla
            var equipo = new List<List<String>>();
            cat = cat.OrderBy(t => t.NOMBRE);
            foreach (var y in cat)
            {
                bool existeCategoria = false;
                List<String> temp = new List<String>();
                foreach (var x in equipo_sol)
                {
                    if (x.TIPO != null)
                    {
                        if (x.TIPO == y.NOMBRE)
                        {
                            temp.Add(y.NOMBRE);
                            existeCategoria = true;
                            if (x.CANTIDAD != 0) { temp.Add(x.CANTIDAD.ToString()); } else { temp.Add("0"); }
                            if (x.CANTAP != 0) { temp.Add(x.CANTAP.ToString()); } else { temp.Add("0"); }
                            break;
                        }
                    }
                    else
                    {
                        temp.Add("");
                        temp.Add("");
                        temp.Add("");
                    }
                }
                if (!existeCategoria)
                {
                    temp.Add(y.NOMBRE);
                    temp.Add("0");
                    temp.Add("0");

                }
                equipo.Add(temp);
            }
            //envia la informacion del equipo solicitado a modo de ViewBag
            ViewBag.Equipo_Solict = equipo;
            return View(pRESTAMO);
        }


        //Requiere: Un objeto prestamo, id del prestamo a modificar, int[] cantidad que dice las cantidades de las categorias de ahora.
        //Modifica: Actualiza en la base de datos la informacion relacionada con ese prestamo.
        //Retorna: vista con los campos para editar solicitud.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Solicitar préstamos,Aceptar préstamos,superadmin")]
        public ActionResult Edit([Bind(Include = "ID,NUMERO_BOLETA,MOTIVO,FECHA_SOLICITUD,FECHA_RETIRO,PERIODO_USO,SOFTWARE_REQUERIDO,OBSERVACIONES_SOLICITANTE,OBSERVACIONES_APROBADO,OBSERVACIONES_RECIBIDO,CEDULA_USUARIO,SIGLA_CURSO")] PRESTAMO p, string id, int[] cantidad, string b, String Fecha_Inicio_Curso)
        {
            //Busca el prestamo en la base de datos
            PRESTAMO P = db.PRESTAMOS.Find(id);

            //Busca el equipo previamente solicitado
            var equipo_sol = from o in db.PRESTAMOS
                             from o2 in db.EQUIPO_SOLICITADO
                             where (o.ID == id && o2.ID_PRESTAMO == id)
                             select new { ID = o.ID, ID_EQUIPO = o2.ID_PRESTAMO, TIPO = o2.TIPO_ACTIVO, CANTIDAD = o2.CANTIDAD, CANTAP = o2.CANTIDADAPROBADA };

            //Determina las categorias de activos presentes en el sistema
            var cat = (from ac in db.ACTIVOS
                       from t in db.TIPOS_ACTIVOS
                       where ac.PRESTABLE.Equals(true) && t.ID.Equals(ac.TIPO_ACTIVOID)
                       select new { t.NOMBRE, t.ID }).Distinct();
            cat = cat.OrderBy(t => t.NOMBRE);
            int a = 0;

            //Para guardar cambios en la tabla de equipo solicitado
            foreach (var y in cat)
            {
                bool noEsta = true;
                foreach (var x in equipo_sol)
                {
                    if (y.NOMBRE == x.TIPO)
                    {
                        EQUIPO_SOLICITADO pr = db.EQUIPO_SOLICITADO.Find(id, y.NOMBRE, x.CANTIDAD);

                        //busca si el elemento de la tabla equipo solicitado existe
                        if (pr == null)
                        {
                            //Si no existe lo crea
                            if (cantidad[a] > 0)
                            {
                                pr = new EQUIPO_SOLICITADO();
                                pr.ID_PRESTAMO = id;
                                pr.TIPO_ACTIVO = y.NOMBRE;
                                pr.CANTIDAD = cantidad[a];
                                pr.TIPOS_ACTIVOSID = y.ID;
                                //Lo agrega a la tabla
                                if (ModelState.IsValid)
                                {
                                    db.EQUIPO_SOLICITADO.Add(pr);
                                    db.SaveChanges();
                                }
                            }
                        }
                        else
                        {
                            //Si si existe, lo modifica y guarda los cambios
                            EQUIPO_SOLICITADO eq = new EQUIPO_SOLICITADO();
                            decimal temp = cantidad[a];
                            if (temp > 0)
                            {
                                noEsta = false;
                                eq.ID_PRESTAMO = pr.ID_PRESTAMO;
                                eq.TIPO_ACTIVO = y.NOMBRE;
                                eq.CANTIDAD = temp;
                                eq.CANTIDADAPROBADA = pr.CANTIDADAPROBADA;
                                eq.TIPOS_ACTIVOSID = y.ID;
                                db.EQUIPO_SOLICITADO.Remove(pr);
                                db.SaveChanges();
                                if (ModelState.IsValid)
                                {
                                    db.EQUIPO_SOLICITADO.Add(eq);
                                    db.SaveChanges();
                                }
                            }
                            else
                            {
                                db.EQUIPO_SOLICITADO.Remove(pr);
                                db.SaveChanges();
                            }
                        }
                    }
                }

                if (noEsta)
                {
                    if (cantidad[a] > 0)
                    {
                        //Si no se ha guardado en la tabla anteriormente lo crea y lo guarda
                        EQUIPO_SOLICITADO pr = new EQUIPO_SOLICITADO();
                        pr.ID_PRESTAMO = id;
                        pr.TIPO_ACTIVO = y.NOMBRE;
                        pr.TIPOS_ACTIVOSID = y.ID;
                        pr.CANTIDAD = cantidad[a];
                        if (ModelState.IsValid)
                        {
                            db.EQUIPO_SOLICITADO.Add(pr);
                            db.SaveChanges();
                        }
                    }
                }
                a++;
            }

            ViewBag.Mensaje = "El préstamo ha sido aprobado con éxito";

            var lista = from o in db.PRESTAMOS
                        from o2 in db.ActivosUsers
                        where o.ID == id
                        select new { Prestamo = o, CEDULA = o2.Cedula, USUARIO = o2.Nombre };

            foreach (var m in lista)
            {
                if (m.Prestamo.ID == id)
                {
                    if (m.Prestamo.USUARIO_SOLICITA == m.CEDULA)
                    {
                        var t = new Tuple<string>(m.USUARIO);
                        ViewBag.Nombre = t.Item1;
                    }
                }
            }
            ViewBag.fechSol = P.FECHA_SOLICITUD.ToShortDateString();

            //Guarda los cambios de los otros atributos de la tabla prestamo
            P.MOTIVO = p.MOTIVO;
            P.OBSERVACIONES_SOLICITANTE = p.OBSERVACIONES_SOLICITANTE;
            P.PERIODO_USO = p.PERIODO_USO;

            //P.SIGLA_CURSO = p.SIGLA_CURSO;
            int idCurso2 = 0;
            var idCourse = 0;
            if (p.SIGLA_CURSO != null)
            {
                var course = db.V_COURSES.SingleOrDefault(c => c.COURSES_CODE == p.SIGLA_CURSO);
                if ((course != null))
                {
                    idCourse = course.COURSES;
                }
                P.V_COURSESCOURSES = idCourse;
            }
            else
            {
                P.V_COURSESCOURSES = 0;
                P.FECHA_RETIRO = p.FECHA_RETIRO;
            }
            /*if (p.SIGLA_CURSO == "")
            {

            }*/
            if ((P.V_COURSESCOURSES == p.V_COURSESCOURSES)&&(P.V_COURSESCOURSES != 0))
            {
                //P.FECHA_RETIRO = p.FECHA_RETIRO;
            }
            else
            {
                if (P.V_COURSESCOURSES != 0)
                {
                    P.FECHA_RETIRO = DateTime.ParseExact(Fecha_Inicio_Curso, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    //int rrr = Convert.ToInt32(p.SIGLA_CURSO);
                    //var course = db.V_COURSES.SingleOrDefault(c => Convert.ToString(c.COURSES) == p.SIGLA_CURSO);
                    //var course = db.V_COURSES.SingleOrDefault(c => c.COURSES_CODE == p.SIGLA_CURSO);
                    //var idCourse = course.COURSES;
                    //P.V_COURSESCOURSES = idCourse;
                }
                else
                {
                    P.FECHA_RETIRO = p.FECHA_RETIRO;
                    //P.V_COURSESCOURSES = 0;
                }
                /*
                if (p.SIGLA_CURSO != null)
                {
                    if ()
                        var course = db.V_COURSES.SingleOrDefault(c => Convert.ToString(c.COURSES) == p.SIGLA_CURSO);
                    var idCourse = course.COURSES;
                    P.V_COURSESCOURSES = idCourse;
                }*/
            }

            P.SOFTWARE_REQUERIDO = p.SOFTWARE_REQUERIDO;
            P.Estado = 1;
            if (ModelState.IsValid)
            {
                db.Entry(P).State = EntityState.Modified;
                db.SaveChanges();

                //Envia los correos de notificacion al encargado y al cliente que realizo la solicitud
                emailCliente(P.ID, 2);
                //emailEncargado(P.ID, 2);

                //Redirecciona al historial
                return RedirectToAction("Historial");
            }
            return View(P);
        }



        //GET: PRESTAMOes/Delete/5
        //Requiere: id del Préstamo
        //Modifica: Se encarga de cambiar el estado de la solicitud en la base de datos para que en prestamo aparezca cancelado.
        //Retorna: Vista con el resultado de dicha modificación en la base de datos.
        [Authorize(Roles = "Solicitar préstamos,Aceptar préstamos,superadmin")]
        public ActionResult Delete(string id)
        {
            //Si no entra al cancelar de una solicitud en especifico da error
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            //Busca el prestamo que se quiere cancelar
            PRESTAMO pRESTAMO = db.PRESTAMOS.Find(id);

            //Si entra a cancelar una solicitud que no existe, da error
            if (pRESTAMO == null)
            {
                return HttpNotFound();
            }

            //Para determinar el estado en que se encuentra la solicitud en este momento
            ViewBag.Estadillo = "";
            ViewBag.Estadillo = "Cancelada";

            //Para determinar cual usuario fue el que hizo la solicitud
            var lista = from o in db.PRESTAMOS
                        from o2 in db.ActivosUsers
                        where o.ID == id
                        select new { Prestamo = o, CEDULA = o2.Cedula, USUARIO = o2.Nombre };

            foreach (var m in lista)
            {
                if (m.Prestamo.ID == id)
                {
                    if (m.Prestamo.USUARIO_SOLICITA == m.CEDULA)
                    {
                        var t = new Tuple<string>(m.USUARIO);
                        ViewBag.Nombre = t.Item1;
                    }
                }
            }
            //Para determinar las categorias de activos que existen
            var cat = (from ac in db.ACTIVOS
                       from t in db.TIPOS_ACTIVOS
                       where ac.PRESTABLE.Equals(true) &&
                              t.ID.Equals(ac.TIPO_ACTIVOID)
                       select new { t.NOMBRE, t.ID }).Distinct();

            //Para desplegar las cantidades solicitadas de cada categoria por ese usuario en esa solicitud especifica
            var equipo_sol = from o in db.PRESTAMOS
                             from o2 in db.EQUIPO_SOLICITADO
                             where (o.ID == id && o2.ID_PRESTAMO == id)
                             select new { ID = o.ID, ID_EQUIPO = o2.ID_PRESTAMO, TIPO = o2.TIPO_ACTIVO, CANTIDAD = o2.CANTIDAD, CANTAP = o2.CANTIDADAPROBADA };

            //Para crear la lista de equipo con cantidades que se va a desplegar en la tabla de equipo solicitado que se 
            //podra observar en la pantalla de cancelar (llamada Delete)
            var equipo = new List<List<String>>();
            foreach (var y in cat)
            {
                bool existeCategoria = false;
                List<String> temp = new List<String>();
                foreach (var x in equipo_sol)
                {
                    if (x.TIPO != null)
                    {
                        if (x.TIPO == y.NOMBRE)
                        {
                            temp.Add(y.NOMBRE);
                            existeCategoria = true;
                            if (x.CANTIDAD != 0) { temp.Add(x.CANTIDAD.ToString()); } else { temp.Add("0"); }
                            if (x.CANTAP != 0) { temp.Add(x.CANTAP.ToString()); } else { temp.Add("0"); }
                            break;
                        }
                    }
                    else
                    {
                        temp.Add("");
                        temp.Add("");
                        temp.Add("");
                    }
                }
                if (!existeCategoria)
                {
                    temp.Add(y.NOMBRE);
                    temp.Add("0");
                    temp.Add("0");
                }
                equipo.Add(temp);
            }

            //la variable en la que se guarda la informacion a desplegarse en la tabla de equipo solicitado de la pantalla de cancelacion
            ViewBag.Equipo_Solict = equipo;
            return View(pRESTAMO);
        }

        // POST: PRESTAMOes/Delete/5
        //Requiere: id del Préstamo
        //Modifica: Se encarga de cambiar el estado de la solicitud en la base de datos para que en prestamo aparezca cancelado.
        //Retorna: Vista con el resultado de dicha modificación en la base de datos.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Solicitar préstamos,Aceptar préstamos,superadmin")]
        public ActionResult DeleteConfirmed(string id)
        {
            //busca el objeto PRESTAMO que tenga el id correspondiente
            PRESTAMO pRESTAMO = db.PRESTAMOS.Find(id);
            //Pone el estado del prestamos seleccionado en 6 (cancelado)
            pRESTAMO.Estado = 6;
            //Verifica que el modelo siga siendo valido despues del cambio
            if (ModelState.IsValid)
            {
                //Pone el estado del objeto prestamo seleccionado en modificado
                db.Entry(pRESTAMO).State = EntityState.Modified;
                //guarda los cambios en la base
                db.SaveChanges();
                //Enviar email notificando que se cancelo la solicitud

                emailCliente(pRESTAMO.ID, 3);
                //emailEncargado(pRESTAMO.ID, 3);

                //Redirecciona la pagina al historial
                return RedirectToAction("Historial");
            }

            //Redirecciona la pagina al historial
            return RedirectToAction("Historial");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public enum Estadito
        {
            //Todos,
            Pendiente,
            Aprobada,
            Denegada,
            Abierta,
            Cerrada,
            Cancelada
        }

        // GET: PRESTAMOes/Devolucion
        //Requiere: id del Préstamo
        //Modifica: Se encarga de mostrar la información del préstamo al que pertenece el id, junto con una tabla que muestra
        //el número de activos solicitado y aprobado por categoría. La tabla también muestra botones para desplegar un modal
        //que permite visualizar los activos individuales por categoría
        //Retorna: Vista de Devolucion 
        [Authorize(Roles = "Aceptar préstamos,superadmin")]
        public ActionResult Devolucion(string id)
        {
            if (id == null) //checkea que se reciba un id de préstamo válido
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PRESTAMO pRESTAMO = db.PRESTAMOS.Find(id);//se recupera el préstamo
            if (pRESTAMO == null)
            {
                return HttpNotFound();
            }

            /*  -------------------------------------------------------------------------------------------  */
            //consulta con la información de las categorías solicitadas por préstamo, el número de activos por categoría y el número de activos aprobado
            var equipo_sol = from o in db.PRESTAMOS
                             from o2 in db.EQUIPO_SOLICITADO
                             where o.ID == id && o2.CANTIDAD > 0
                             select new { ID = o.ID, ID_EQUIPO = o2.ID_PRESTAMO, TIPO = o2.TIPO_ACTIVO, CANTIDAD = o2.CANTIDAD, CANTAP = o2.CANTIDADAPROBADA };

            //diccionario de listas de listas
            //guardará como clave la categoría del activo y como valor una lista de listas que trae en cada una la información individual de cada activo
            var equipo_cat = new Dictionary<String, List<List<String>>>();

            List<String> listaAc = listaActivos(equipo_cat);
            //consulta con las categorías solicitadas
            var categorias_sol = from p in db.PRESTAMOS
                                 from e in db.EQUIPO_SOLICITADO
                                 where p.ID == id && e.ID_PRESTAMO == p.ID
                                 select new { CAT = e.TIPO_ACTIVO, TIPO = e.TIPOS_ACTIVOSID };

            //se recorren las categorías para obtener los activos específicos de esa categoría con el id del préstamo, se llama al método equipoPorCategoria
            var observaciones = new Dictionary<String, List<String>>();
            foreach (var c in categorias_sol)
            {
                var eq = new List<List<String>>();
                var ob = new List<String>();
                eq = equipoPorCategoria(c.TIPO, id);
                equipo_cat.Add(c.CAT.ToString(), eq);
                ob = traerObservaciones(id, c.TIPO, pRESTAMO.NUMERO_BOLETA);
                observaciones.Add(c.CAT.ToString(), ob);
             
            }


            var equipo = new List<List<String>>();
            //a partir de equipo_sol arma la información para la tabla de categorías
            foreach (var x in equipo_sol)
            {
                if (x.ID == id)
                {
                    if (x.ID == x.ID_EQUIPO)
                    {
                        List<String> temp = new List<String>();
                        if (x.TIPO != null) { temp.Add(x.TIPO.ToString()); } else { temp.Add(""); }
                        if (x.CANTIDAD != 0) { temp.Add(x.CANTIDAD.ToString()); } else { temp.Add(""); }
                        if (x.CANTAP != 0) { temp.Add(x.CANTAP.ToString()); } else { temp.Add(""); }
                        equipo.Add(temp);
                    }
                }
            }

            //se pasan las variables a la controladora 
            ViewBag.Equipo_Solict = equipo;
            ViewBag.EquipoPorCat = equipo_cat;
            ViewBag.Observaciones = observaciones;
            //se desea mantener el diccionario aún después del post
            TempData["activos"] = equipo_cat;
            TempData["observaciones"] = observaciones;
            TempData.Keep();

            return View(pRESTAMO);
        }



        //POST: PRESTAMOes/Devolucion
        //Requiere: id del Préstamo, información de checkbox de los modales y de las tablas, string con observaciones 
        //Modifica: Se encarga de enviar la información sobre la devolución a la base datos indicando que activos específicamente van a ser 
        //aceptados como devueltos
        //Retorna: Vista de Devolucion con la información de los modales actualizados.
        [HttpPost]
        [Authorize(Roles = "Aceptar préstamos,superadmin")]
        public ActionResult Devolucion(string ID, bool[] column5_checkbox, bool column5_checkAll, string b, string OBSERVACIONES_APROBADO, bool[] activoSeleccionado, String[] Notas)
        {
            //Se recupera al préstamo y se le actualiza el campo de observaciones_aprobado
            PRESTAMO pRESTAMO = db.PRESTAMOS.Find(ID);
            pRESTAMO.OBSERVACIONES_APROBADO = OBSERVACIONES_APROBADO;

            //Se recupera el diccionario después del post
            Dictionary<String, List<List<String>>> dic = (Dictionary<String, List<List<String>>>)TempData["activos"];
            Dictionary<String, List<String>> observaciones = (Dictionary<String, List<String>>)TempData["observaciones"];
            ViewBag.Observaciones = observaciones;
            List<String> idPrestados = new List<String>();
            List<String> lista = listaActivos(dic);

            //Lista que guardará las placas de todos los activos de un préstamo.
            //Se usará para revisar si el activo al final está devuelto o no.
            Dictionary<String, bool> listaDevueltos = new Dictionary<String, bool> ();
            //se guardan los ids de los activos del préstamo
            foreach (KeyValuePair<String, List<List<String>>> entrada in dic)
            {
                foreach (List<String> l in entrada.Value)
                {
                    idPrestados.Add(l[3]);
                }
            }

            //guarda cambios efectuados en base
            if (ModelState.IsValid)
            {
                db.Entry(pRESTAMO).State = EntityState.Modified;
                db.SaveChanges();
            }


            //se recupera el préstamo junto con sus activos 
            var prestamo = db.PRESTAMOS.Include(i => i.EQUIPO_SOLICITADO).SingleOrDefault(h => h.ID == ID);
            var equipo_sol = prestamo.EQUIPO_SOLICITADO;
            var activos_asignados = prestamo.ACTIVOes;

            /*---------------------------------------------------------------------------*/
            //checkea si se apretó el botón de "actualizar devolución"
            if (b == "Actualizar devolución")
            {
                //Caso de que se seleccione el checkbox Devolver todos, entonces se marca cada activo del préstamo como devuelto
                if (column5_checkAll)
                {
                    foreach (var y in equipo_sol)
                    {
                        foreach (var x in activos_asignados)
                        {
                            if (x.TIPO_ACTIVOID == y.TIPOS_ACTIVOSID)
                            {
                                x.ESTADO_PRESTADO = 0;
                                int indice = indiceActivo(lista, x.PLACA);
                                new TransaccionesController().CreatePrestamo(User.Identity.GetUserName(), "Devuelto de préstamo", "Se devuelve activo en prestamo", x.ID, unchecked((int)prestamo.NUMERO_BOLETA), pRESTAMO.FECHA_RETIRO, DateTime.Now.Date, Notas[indice], pRESTAMO.USUARIO_SOLICITA);
                            }
                        }
                    }
                    //Al devolverse todos los activos, el estado del préstamo pasa a ser "Cerrado"
                    pRESTAMO.Estado = 5;
                    db.SaveChanges();
                    return RedirectToAction("Index");

                    // return RedirectToAction("Details", new { id = ID });
                }
                else 
                {
                    bool todos = true;
                    if (hayFilaEntera(column5_checkbox))//Revisa si hay algún check para devolver todos los activos de una sola categoría
                    {
                        int cont = 0;
                        //corrige el array de booleanos recuperado
                        List<bool> devolverCheck = corregirVectorBool(column5_checkbox);

                        foreach (var y in equipo_sol)
                        {
                            bool t = devolverCheck[cont];
                            if (t)
                            { //si fueron todos seleccionados en esa fila, de ese tipo entonces se marca como devuelto cada activo

                                String cat = dic.Keys.ElementAt(cont);
                                foreach (List<String> l in dic[cat])
                                {
                                    String id = l[3];
                                    ACTIVO act = db.ACTIVOS.Find(id);
                                    int indice = indiceActivo(lista, act.PLACA);
                                    if (act.ESTADO_PRESTADO != 0)
                                    {
                                        act.ESTADO_PRESTADO = 0;
                                        new TransaccionesController().CreatePrestamo(User.Identity.GetUserName(), "Devuelto de préstamo", "Se devuelve activo en prestamo", act.ID, unchecked((int)prestamo.NUMERO_BOLETA), pRESTAMO.FECHA_RETIRO, DateTime.Now.Date, Notas[indice], pRESTAMO.USUARIO_SOLICITA);
                                    }
                                    db.Entry(act).State = EntityState.Modified;
                                    db.SaveChanges();
                                }

                            }
                            cont++;
                        }
                    }

                    //corrige el vector de booleanos recuperado
                    List<bool> devolucionActivos = corregirVectorBool(activoSeleccionado);
                    for (int i = 0; i < idPrestados.Count(); i++)
                    {
                        String id = idPrestados[i];
                        ACTIVO act = db.ACTIVOS.Find(id);
                        if (devolucionActivos[i])
                        {
                            int indice = indiceActivo(lista, act.PLACA);
                            if (act.ESTADO_PRESTADO != 0)
                            {
                                act.ESTADO_PRESTADO = 0;
                                new TransaccionesController().CreatePrestamo(User.Identity.GetUserName(), "Devuelto de préstamo", "Se devuelve activo en prestamo", act.ID, unchecked((int)prestamo.NUMERO_BOLETA), pRESTAMO.FECHA_RETIRO, DateTime.Now.Date, Notas[indice], pRESTAMO.USUARIO_SOLICITA);
                            }
                        }
                        else if (act.ESTADO_PRESTADO != 0)
                        {                         
                            
                                todos = false;
                        }
                        db.Entry(act).State = EntityState.Modified;
                        db.SaveChanges();
                    }

                    if (todos)
                    {
                        pRESTAMO.Estado = 5;
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                }

                if (ModelState.IsValid)
                {
                    db.Entry(pRESTAMO).State = EntityState.Modified;
                    db.SaveChanges();
                }
                bool hasErrors = ViewData.ModelState.Values.Any(x => x.Errors.Count > 1);
                if (!hasErrors)
                {
                    ViewBag.Mensaje = "Los activos han sido devueltos correctamente.";
                }
                else
                {
                    ViewBag.Mensaje2 = "Los activos no han sido devueltos correctamente.";
                }

            }
            return RedirectToAction("Devolucion", new { id = ID });
        }


        // Requiere: valor seleccionado en el dropdown de Categoría, valor del botón seleccionado, valor de la fecha inicial y la fecha final
        // Modifica: se encarga de llenar la tabla de Inventario, de la categoría que recibe cómo parámetro (en el modal).
        // Regresa: N/A.
        private List<List<String>> llenarTablaDetails(String Categoria)
        {

            int tipo = int.Parse(Categoria);
            var activos_enCat = new List<List<String>>();
            //Se seleccionan los activos que sean de la misma categoria
            var activos = db.ACTIVOS.Where(c => c.TIPO_ACTIVOID == tipo);
            foreach (Activos_PrestamosOET.Models.ACTIVO x in activos)
            {
                //Se verifica que el activo sea prestable y que no este en prestamo actualmente
                if (Categoria.Equals(x.TIPO_ACTIVOID.ToString()) && x.PRESTABLE == true && x.ESTADO_PRESTADO == 0)
                {
                    List<String> temp = new List<String>();
                    if (x.FABRICANTE != null) { temp.Add(x.FABRICANTE); } else { temp.Add(""); }
                    if (x.MODELO != null) { temp.Add(x.MODELO); } else { temp.Add(""); }
                    if (x.PLACA != null) { temp.Add(x.PLACA); } else { temp.Add(""); }

                    activos_enCat.Add(temp);
                }
            }

            //Se maneja el caso en que una categoria no tenga activos disponibles
            if (activos_enCat.Count == 0)
            {
                List<String> temp = new List<String>();
                temp.Add("");
                temp.Add("");
                temp.Add("");
                activos_enCat.Add(temp);
                ViewBag.NoActivos = "No hay Activos Prestables con esta categoría.";
            }
            return activos_enCat;
        }

        //Requiere: Recibe una categoria de activo especifica y un id de un prestamo.
        //Modifica: Busca los activos de una categoria especifica, que estan asociados al prestamo con id igual a "id"
        //Regresa: Retorna un conjunto de listas de string, la cua cada una contiene: fabricante, modelo y placa; de cada activo de una categoria especifica, que estan asociados a un prestamo.
        private List<List<String>> llenarTablaDetails(String Categoria, string id)
        {


            var activos_enCat = new List<List<String>>();
            var activos = db.PRESTAMOS.Include(i => i.ACTIVOes).SingleOrDefault(h => h.ID == id);
            foreach (ACTIVO x in activos.ACTIVOes)//Se itera sobre cada uno de los activos asociados a un prestamo.
            {

                if (Categoria.Equals(x.TIPO_ACTIVOID.ToString()))
                {
                    List<String> temp = new List<String>();
                    if (x.FABRICANTE != null) { temp.Add(x.FABRICANTE); } else { temp.Add(""); }
                    if (x.MODELO != null) { temp.Add(x.MODELO); } else { temp.Add(""); }
                    if (x.PLACA != null) { temp.Add(x.PLACA); } else { temp.Add(""); }

                    activos_enCat.Add(temp);
                }
            }

            return activos_enCat;
        }

        //Requiere: Necesita las placas de los activos  que se van a agregar a un prestamo. Tambien ocupa el id del prestamo al que se agregaran los cativos.
        //Modifica: Identifica los numeros de placas de los activos que seran asociados a un prestamo. Luedo los agrega y cambia el estado del prestamo.
        //Regresa: N/A
        protected void addActivosToPrestamo(string[] placas, string id)
        {
            LinkedList<ACTIVO> activosPorAgregar = new LinkedList<ACTIVO>();
            var prestamo = db.PRESTAMOS.Include(i => i.ACTIVOes).SingleOrDefault(h => h.ID == id);
            foreach (string p in placas)
            {
                if (p != "false")//En caso de que no sea false, "p" sera igual a un numero de placa
                {
                    var activo = db.ACTIVOS.SingleOrDefault(i => i.PLACA == p);//Se encuentra el activo que tiene la placa igual a "p"
                    activo.ESTADO_PRESTADO = 1;//Se cambia el estado del activo para que se sepa que esta prestado.

                    prestamo.Estado = 4;//Se cambia el estado del prestamo a "Abierto"
                    activosPorAgregar.AddLast(activo);//Se agrega el prestamo a la lista de Activos
                    prestamo.ACTIVOes.Add(activo);//Se agrega el activo a la lista para prestamo.
                    if (ModelState.IsValid)
                    {
                        db.Entry(activo).State = EntityState.Modified;
                        db.Entry(prestamo).State = EntityState.Modified;
                        db.SaveChanges();

                        new TransaccionesController().CreatePrestamo(User.Identity.GetUserName(), "Asignado en Préstamo", "Sale asignado a un préstamo", activo.ID, unchecked((int)prestamo.NUMERO_BOLETA), prestamo.FECHA_RETIRO, prestamo.FECHA_RETIRO.AddDays(prestamo.PERIODO_USO), "", prestamo.USUARIO_SOLICITA);
                    }
                    else
                    {
                        var errors = ModelState.Values.SelectMany(v => v.Errors);
                    }

                }
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
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext,
                                                                         viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View,
                                             ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }

        //Requiere: el id del prestamo 
        //Modifica: se encarga de llamar a la vista que luego se convertira en la boleta imprimible de un prestamo
        //Regresa: la vista
        public ActionResult BoletaPDF(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PRESTAMO pRESTAMO = db.PRESTAMOS.Find(id);
            if (pRESTAMO.SIGLA_CURSO != null)
            {
                ViewBag.ConCurso = pRESTAMO.SIGLA_CURSO;
            }

            if (pRESTAMO.SIGLA_CURSO != null)
            {
                ViewBag.ConCurso = pRESTAMO.SIGLA_CURSO;
            }

            return View(pRESTAMO);
        }

        //Requiere: el id del prestamo 
        //Modifica: Crea la vista de DetallesPDF. El usuario no va a ver esta vista, si no que es para despues convertirla en un string y enviarla por correo electronico
        //Regresa: la vista
        public ActionResult DetallesPDF(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PRESTAMO pRESTAMO = db.PRESTAMOS.Find(id);
 
            return View(pRESTAMO);
        }

        //Requiere: el id del curso 
        //Modifica: Consulta en base de datos asincrónicamente para obtener las fechas del curso seleccionado
        //Regresa: lista con fechas y número de días de diferencia
        public ActionResult obtenerFechasCurso(string idCurso)
        {
            var fechas = from c in db.V_COURSES
                         where idCurso == c.COURSES_CODE
                         select new { Fecha_Inicio = c.START_DATE, Fecha_Fin = c.END_DATE };
            //hay que cambiar obviamente a las fechas, cuando se actualice el modelo
            List<String> listaDatos = new List<string>();

            foreach (var v in fechas)
            {
                //String inicio = v.Fecha_Inicio.ToString("MM/dd/yyyy"); ;
                String inicio = ((DateTime)v.Fecha_Inicio).ToString("dd/MM/yyyy");
                String fin = ((DateTime)v.Fecha_Fin).ToString("dd/MM/yyyy");

                double dias = (((DateTime)v.Fecha_Fin) - ((DateTime)v.Fecha_Inicio)).TotalDays;
                listaDatos.Add(inicio);
                listaDatos.Add(fin);
                listaDatos.Add(dias.ToString());
            }
            return Json(listaDatos);
        }
    }
}
