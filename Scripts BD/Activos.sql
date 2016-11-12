------------------------------------
-- Vista de Usuarios desde activos
------------------------------------
 GRANT SELECT ANY TABLE TO "ACTIVOS" WITH ADMIN OPTION;
 create or replace view activos.v_usuarios as
 select * from RESERVAS.usuarios;
-----------------------------------------------------------------------------------
-- Vista del documento de tipo de cambio (para conversiones de dolares a colones)
-----------------------------------------------------------------------------------
 GRANT SELECT ANY TABLE TO "ACTIVOS" WITH ADMIN OPTION;
 create or replace view activos.v_tipo_cambio as
 select * from FINANCIERO.DOCUMENTO_TIPOCAMBIO;
------------------------------------------------------
-- Vista de los empleados (Para asignarles activos)
------------------------------------------------------
GRANT SELECT ANY TABLE TO "ACTIVOS" WITH ADMIN OPTION;

CREATE OR REPLACE FORCE VIEW "V_EMPLEADOS"  AS

 SELECT DISTINCT

   EP.IDSEDE AS ESTACION_ID,

   ep.IDEMPLEADO,

   emp.nombre || ' ' || emp.apellidos as Nombre,

   emp.EMAIL,

   emp.ESTADO



 FROM

   FINANCIERO.EMPLEADOS  Emp,

   FINANCIERO.EMPLEADO_PUESTO   ep,

   V_ANFITRIONA  an,

   V_ESTACION  es

 WHERE

 emp.IDEMPLEADO = ep.IDEMPLEADO

 AND EP.IDSEDE       = es.ID

 AND EP.IDEMPRESA    = an.ID;

// Fin de las vistas

CREATE SEQUENCE INSERT_TIPO_ACTIVO
  START WITH 1
  INCREMENT BY 1
  NOCACHE
  NOCYCLE;
/

CREATE OR REPLACE TRIGGER id_tipos_activos
  BEFORE INSERT ON TIPOS_ACTIVOS
  FOR EACH ROW
BEGIN
  SELECT INSERT_TIPO_ACTIVO.nextval INTO :new.Id from dual;
END;
/
CREATE SEQUENCE INSERT_TIPO_TRANSACCION
  START WITH 1
  INCREMENT BY 1
  NOCACHE
  NOCYCLE;
/
CREATE OR REPLACE TRIGGER id_tipo_transaccion
    BEFORE INSERT ON tipos_transacciones
    FOR EACH ROW
  BEGIN
    SELECT INSERT_TIPO_TRANSACCION.nextval INTO :new.ID from dual;
  END;
  /

  CREATE SEQUENCE INSERT_ESTADO_ACTIVO
  START WITH 1
  INCREMENT BY 1
  NOCACHE
  NOCYCLE;
/

CREATE OR REPLACE TRIGGER id_estado_activo
    BEFORE INSERT ON estados_activos
    FOR EACH ROW
  BEGIN
    SELECT INSERT_ESTADO_ACTIVO.nextval INTO :new.ID from dual;
  END;
  /

  CREATE SEQUENCE INSERT_TRANSACCION
  START WITH 1
  INCREMENT BY 1
  NOCACHE
  NOCYCLE;
  /

  CREATE OR REPLACE TRIGGER id_transaccion
    BEFORE INSERT ON transacciones
    FOR EACH ROW
  BEGIN
    SELECT INSERT_TRANSACCION.nextval INTO :new.ID from dual;
  END;
  /

  CREATE SEQUENCE INSERT_CENTRO_COSTOS
  START WITH 1
  INCREMENT BY 1
  NOCACHE
  NOCYCLE;
  /

  CREATE OR REPLACE TRIGGER ID_CENTRO_COSTOS
    BEFORE INSERT ON centros_de_costos
    FOR EACH ROW
  BEGIN
    SELECT INSERT_CENTRO_COSTOS.nextval INTO :new."Id" from dual;
  END;
  /


  CREATE SEQUENCE
  INSERT_NUMERO_BOLETA
  START WITH 1
  INCREMENT BY 1
  NOCACHE
  NOCYCLE
  /

  CREATE OR REPLACE TRIGGER num_boleta
  BEFORE INSERT ON PRESTAMOS
  FOR EACH ROW
  BEGIN
  SELECT INSERT_NUMERO_BOLETA.nextval
  INTO :new.NUMERO_BOLETA from dual;
  END;
  /

  CREATE OR REPLACE FUNCTION add_days (d date, n number)
  RETURN DATE
  IS
  v_date DATE;
  BEGIN
  v_date := d + n;

  DBMS_OUTPUT.put_line (v_date);
  RETURN v_date;
  END;
/
  commit;
