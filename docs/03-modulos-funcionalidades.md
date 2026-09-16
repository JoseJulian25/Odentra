# Módulos y Funcionalidades

## 1. Objetivo

Este documento define los módulos funcionales que componen el Sistema de Gestión para Clínica Dental y las funcionalidades que estarán disponibles dentro de cada uno.

La finalidad es establecer el comportamiento funcional esperado del sistema antes de iniciar la implementación.

El documento servirá como referencia para:

* Diseño de la base de datos.
* Diseño de las interfaces.
* Definición de casos de uso.
* Implementación de la lógica de negocio.
* Distribución del trabajo entre los integrantes del equipo.
* Generación de código mediante herramientas de inteligencia artificial.
* Elaboración de pruebas.

---

# 2. Estructura general de módulos

El sistema estará compuesto inicialmente por los siguientes módulos:

```text
Sistema de Gestión Dental
│
├── Dashboard
│
├── Pacientes
├── Odontólogos
├── Citas
├── Historia Clínica
├── Odontograma
├── Tratamientos
├── Pagos
│
├── Usuarios
├── Roles y Permisos
├── Auditoría
│
└── Reportes
```

El Dashboard funcionará como punto de entrada para los usuarios autenticados y mostrará información acorde con sus permisos.

---

# 3. Dashboard

## 3.1. Descripción

El Dashboard será la pantalla principal después del inicio de sesión.

La información mostrada dependerá del rol y los permisos del usuario.

No se pretende desarrollar un sistema avanzado de analítica. El Dashboard tendrá como objetivo proporcionar una vista rápida del estado actual de la clínica.

## 3.2. Información general

Para usuarios administrativos podrá mostrar:

* Cantidad de pacientes activos.
* Cantidad de odontólogos activos.
* Citas programadas para el día.
* Citas pendientes.
* Tratamientos en proceso.
* Pagos pendientes.

También podrá incluir accesos rápidos a operaciones frecuentes.

Ejemplo:

```text
┌─────────────────────────────────────────────────────────┐
│ Dashboard                                               │
├───────────────┬───────────────┬─────────────────────────┤
│ Pacientes     │ Citas hoy     │ Pagos pendientes        │
│     524       │      18       │      RD$ 42,500         │
├───────────────┴───────────────┴─────────────────────────┤
│                                                         │
│ Próximas citas                                         │
│                                                         │
│ 09:00  Juan Pérez       Dr. Gómez       Confirmada     │
│ 10:00  Ana López        Dra. Díaz        Programada     │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

El Dashboard del odontólogo mostrará principalmente:

* Sus citas del día.
* Pacientes atendidos recientemente.
* Tratamientos en proceso.
* Accesos rápidos a sus pacientes.

---

# 4. Módulo de Pacientes

## 4.1. Descripción

Permitirá administrar la información general de los pacientes de la clínica.

## 4.2. Listado de pacientes

La pantalla principal mostrará una tabla con información resumida.

Columnas iniciales:

* Nombre completo.
* Identificación.
* Teléfono.
* Correo.
* Fecha de registro.
* Estado.

Funcionalidades:

* Buscar pacientes.
* Filtrar por estado.
* Consultar información.
* Registrar paciente.
* Editar paciente.
* Desactivar paciente.

La búsqueda podrá realizarse por:

* Nombre.
* Apellido.
* Identificación.
* Teléfono.

## 4.3. Registro de paciente

El formulario permitirá registrar:

### Información personal

* Nombres.
* Apellidos.
* Identificación.
* Fecha de nacimiento.
* Sexo.

### Información de contacto

* Teléfono.
* Correo electrónico.
* Dirección.

### Contacto de emergencia

* Nombre.
* Teléfono.
* Relación con el paciente.

El sistema deberá validar los campos obligatorios y formatos correspondientes.

## 4.4. Consulta del paciente

La información del paciente podrá organizarse mediante pestañas:

```text
Información | Historia Clínica | Odontograma | Tratamientos | Pagos | Citas
```

El contenido disponible dependerá de los permisos del usuario.

La pantalla de información general mostrará los datos administrativos del paciente.

## 4.5. Edición

Los usuarios con permiso podrán modificar la información administrativa del paciente.

La modificación de información clínica se realizará desde los módulos correspondientes.

## 4.6. Desactivación

Un paciente podrá pasar a estado inactivo.

La desactivación no eliminará físicamente sus registros.

Los pacientes inactivos podrán permanecer disponibles para consulta histórica, pero no deberían utilizarse para crear nuevas citas sin ser reactivados.

---

# 5. Módulo de Odontólogos

## 5.1. Descripción

Permitirá administrar la información de los odontólogos que forman parte de la clínica.

## 5.2. Listado

La pantalla mostrará:

* Nombre.
* Especialidad.
* Número de licencia/colegiatura.
* Teléfono.
* Correo.
* Estado.

Funcionalidades:

* Buscar.
* Filtrar.
* Consultar.
* Registrar.
* Editar.
* Desactivar.

## 5.3. Registro

El formulario permitirá introducir:

* Nombres.
* Apellidos.
* Número de licencia/colegiatura.
* Especialidad.
* Teléfono.
* Correo.
* Estado.

Opcionalmente podrá asociarse un usuario existente al odontólogo.

## 5.4. Consulta

La vista detallada podrá mostrar:

* Información del odontólogo.
* Próximas citas.
* Historial de citas.
* Tratamientos realizados.

---

# 6. Módulo de Citas

## 6.1. Descripción

Permitirá gestionar las citas de los pacientes con los odontólogos.

## 6.2. Listado

La pantalla podrá mostrar las citas en formato de tabla.

Columnas:

* Fecha.
* Hora.
* Paciente.
* Odontólogo.
* Motivo.
* Estado.

Se podrán aplicar filtros por:

* Fecha.
* Rango de fechas.
* Paciente.
* Odontólogo.
* Estado.

## 6.3. Registro de cita

El formulario permitirá seleccionar:

* Paciente.
* Odontólogo.
* Fecha.
* Hora.
* Motivo.
* Observaciones.

El sistema validará que:

* El paciente esté activo.
* El odontólogo esté activo.
* La fecha sea válida.
* El odontólogo no tenga otra cita activa en el mismo horario.

## 6.4. Estados

Las citas podrán encontrarse en:

```text
Programada
Confirmada
Atendida
Cancelada
No asistió
```

## 6.5. Modificación

Una cita programada podrá modificarse mientras no haya sido atendida o cancelada.

Podrán modificarse:

* Fecha.
* Hora.
* Odontólogo.
* Motivo.
* Observaciones.

## 6.6. Cancelación

Las citas podrán cancelarse.

La cancelación no eliminará físicamente la cita, sino que cambiará su estado.

## 6.7. Atención

Cuando el paciente sea atendido, la cita podrá marcarse como `Atendida`.

A partir de esta acción, el odontólogo podrá acceder al flujo clínico correspondiente.

---

# 7. Módulo de Historia Clínica

## 7.1. Descripción

Permitirá almacenar la información clínica relevante de cada paciente.

## 7.2. Antecedentes

La historia clínica tendrá inicialmente los siguientes campos:

* Alergias.
* Enfermedades relevantes.
* Medicamentos actuales.
* Antecedentes médicos.
* Antecedentes odontológicos.
* Hábitos.
* Observaciones generales.

## 7.3. Registro y modificación

El odontólogo podrá registrar y actualizar la información clínica.

Las modificaciones importantes deberán quedar registradas mediante auditoría.

## 7.4. Consultas clínicas

La historia clínica tendrá un historial de consultas.

Cada consulta podrá registrar:

* Fecha.
* Odontólogo.
* Motivo.
* Observaciones.
* Diagnóstico.
* Plan de tratamiento.

Las consultas anteriores serán de solo lectura una vez finalizadas, salvo que posteriormente se establezca una funcionalidad explícita para corrección.

---

# 8. Módulo de Odontograma

## 8.1. Descripción

Permitirá visualizar y registrar el estado de las piezas dentales del paciente.

Se utilizará la numeración FDI.

## 8.2. Visualización

El odontograma mostrará las piezas dentales organizadas de acuerdo con su numeración.

Cada pieza tendrá un estado visual que permita identificar su condición.

Estados iniciales:

```text
Sano
Caries
Restauración
Ausente
Fracturado
Tratamiento realizado
```

## 8.3. Selección de pieza

Al seleccionar una pieza dental se mostrará información como:

* Número de pieza.
* Estado actual.
* Observaciones.
* Fecha de última modificación.
* Odontólogo que realizó la modificación.

## 8.4. Actualización

El odontólogo podrá:

* Seleccionar una pieza.
* Cambiar su estado.
* Agregar observaciones.
* Guardar los cambios.

Las modificaciones deberán quedar asociadas al paciente y registradas mediante auditoría.

## 8.5. Alcance

El odontograma será una representación simplificada.

No se contempla inicialmente:

* Representación detallada de superficies dentales.
* Diagnóstico automatizado.
* Radiografías.
* Imágenes médicas.
* Gráficos clínicos avanzados.

---

# 9. Módulo de Tratamientos

## 9.1. Descripción

El módulo tendrá dos funciones principales:

1. Administrar el catálogo de tratamientos.
2. Registrar tratamientos realizados a pacientes.

---

## 9.2. Catálogo de tratamientos

Permitirá administrar los tratamientos disponibles.

Cada tratamiento tendrá:

* Nombre.
* Descripción.
* Precio base.
* Estado.

Ejemplo:

```text
Limpieza dental       RD$ 1,500
Restauración          RD$ 3,000
Extracción            RD$ 2,500
Endodoncia            RD$ 12,000
```

## 9.3. Administración del catálogo

Los usuarios autorizados podrán:

* Crear tratamientos.
* Editar tratamientos.
* Desactivar tratamientos.
* Consultar tratamientos.

Un tratamiento desactivado no podrá utilizarse para nuevos registros, pero permanecerá asociado a los tratamientos históricos.

---

## 9.4. Tratamiento realizado

Un odontólogo podrá registrar un tratamiento realizado a un paciente.

Información:

* Paciente.
* Tratamiento.
* Odontólogo.
* Fecha.
* Pieza dental, cuando corresponda.
* Precio.
* Estado.
* Observaciones.

## 9.5. Estados

Inicialmente:

```text
Pendiente
En proceso
Completado
Cancelado
```

## 9.6. Tratamientos asociados a piezas

Cuando un tratamiento se relacione con una pieza dental específica, se podrá seleccionar la pieza correspondiente del odontograma.

Ejemplo:

```text
Tratamiento:
Restauración

Paciente:
Juan Pérez

Pieza:
36

Estado:
Completado
```

---

# 10. Módulo de Pagos

## 10.1. Descripción

Permitirá registrar y consultar los pagos realizados por los pacientes.

## 10.2. Registro de pago

Un pago deberá estar asociado a un tratamiento realizado.

Información:

* Paciente.
* Tratamiento.
* Fecha.
* Monto.
* Método de pago.
* Observaciones.

## 10.3. Métodos de pago

Inicialmente:

```text
Efectivo
Tarjeta
Transferencia
```

## 10.4. Pagos parciales

Un tratamiento podrá tener múltiples pagos.

Ejemplo:

```text
Tratamiento: Endodoncia

Precio total:       RD$ 12,000

Pago 1:             RD$ 5,000
Pago 2:             RD$ 3,000

Total pagado:       RD$ 8,000
Pendiente:          RD$ 4,000
```

El sistema deberá calcular automáticamente:

```text
Monto pendiente =
Precio del tratamiento - Total de pagos
```

## 10.5. Validaciones

El sistema no deberá permitir registrar un pago cuyo monto sea superior al saldo pendiente.

Una vez cubierto el precio total, el saldo deberá ser cero.

---

# 11. Módulo de Usuarios

## 11.1. Descripción

Permitirá administrar las cuentas que tienen acceso al sistema.

## 11.2. Listado

Mostrará:

* Nombre de usuario.
* Correo.
* Nombre.
* Estado.
* Roles.
* Fecha de creación.

## 11.3. Creación

El Administrador podrá crear usuarios indicando:

* Nombre.
* Correo.
* Contraseña inicial.
* Estado.
* Rol o roles.

Si el usuario será odontólogo, podrá asociarse posteriormente con su registro de odontólogo.

## 11.4. Edición

El Administrador podrá:

* Cambiar información básica.
* Cambiar roles.
* Activar o desactivar usuarios.
* Restablecer credenciales mediante el mecanismo de autenticación implementado.

## 11.5. Desactivación

Un usuario desactivado no podrá iniciar sesión.

Su información histórica deberá conservarse para fines de auditoría.

---

# 12. Módulo de Roles y Permisos

## 12.1. Descripción

Permitirá administrar el acceso funcional del sistema.

## 12.2. Roles

El sistema tendrá inicialmente:

```text
Administrador
Recepcionista
Odontólogo
```

## 12.3. Permisos

Los permisos representarán acciones específicas.

Ejemplos:

```text
Pacientes.Ver
Pacientes.Crear
Pacientes.Editar

Citas.Ver
Citas.Crear
Citas.Editar
Citas.Cancelar

HistoriaClinica.Ver
HistoriaClinica.Editar

Odontograma.Ver
Odontograma.Editar

Pagos.Ver
Pagos.Crear
```

## 12.4. Administración

El Administrador podrá:

* Consultar roles.
* Crear roles.
* Editar roles.
* Consultar permisos.
* Asignar permisos a roles.

La eliminación física de roles no será necesaria inicialmente.

---

# 13. Módulo de Auditoría

## 13.1. Descripción

Permitirá consultar las operaciones relevantes realizadas dentro del sistema.

## 13.2. Información registrada

Cada evento podrá contener:

* Fecha y hora.
* Usuario.
* Acción.
* Módulo.
* Entidad.
* Identificador del registro.
* Descripción.

## 13.3. Acciones auditables

Como mínimo:

* Inicio de sesión.
* Creación de registros.
* Modificación de registros importantes.
* Desactivación de registros.
* Modificaciones de información clínica.
* Cambios en el odontograma.
* Registro de pagos.
* Cambios de roles y permisos.

## 13.4. Consulta

El Administrador podrá buscar y filtrar registros por:

* Usuario.
* Fecha.
* Acción.
* Módulo.
* Entidad.

Los registros de auditoría no podrán ser modificados desde la interfaz.

---

# 14. Módulo de Reportes

## 14.1. Descripción

Permitirá generar consultas resumidas de la información almacenada.

Los reportes serán principalmente informativos y estarán orientados a las necesidades administrativas y clínicas básicas.

## 14.2. Reporte de citas

Filtros:

* Fecha inicial.
* Fecha final.
* Odontólogo.
* Estado.

Información:

* Fecha.
* Hora.
* Paciente.
* Odontólogo.
* Estado.

## 14.3. Reporte de pacientes

Podrá mostrar:

* Total de pacientes.
* Pacientes activos.
* Pacientes inactivos.
* Pacientes registrados durante un período.

## 14.4. Reporte de tratamientos

Podrá mostrar:

* Tratamiento.
* Cantidad realizada.
* Odontólogo.
* Estado.
* Período.

## 14.5. Reporte de pagos

Podrá mostrar:

* Total cobrado.
* Cantidad de pagos.
* Pagos por método.
* Pagos pendientes.
* Ingresos por período.

## 14.6. Reportes por usuario

El contenido disponible dependerá del rol y permisos.

El Administrador tendrá acceso a los reportes generales.

El Recepcionista tendrá acceso a reportes administrativos.

El Odontólogo tendrá acceso a reportes relacionados con su actividad clínica.

---

# 15. Búsqueda y filtros

Los módulos que manejen cantidades considerables de registros deberán permitir búsquedas o filtros apropiados.

No será obligatorio implementar búsqueda avanzada en todas las pantallas.

Como mínimo:

| Módulo       | Búsqueda/Filtros                         |
| ------------ | ---------------------------------------- |
| Pacientes    | Nombre, identificación, teléfono, estado |
| Odontólogos  | Nombre, especialidad, estado             |
| Citas        | Fecha, odontólogo, paciente, estado      |
| Tratamientos | Nombre, estado                           |
| Pagos        | Fecha, paciente, método                  |
| Usuarios     | Nombre, correo, estado, rol              |
| Auditoría    | Usuario, fecha, acción, módulo           |
| Reportes     | Filtros según reporte                    |

---

# 16. Estados y eliminación de registros

El sistema deberá evitar la eliminación física de información que tenga relevancia histórica.

Se utilizará preferentemente un mecanismo de activación/desactivación para entidades como:

* Pacientes.
* Odontólogos.
* Tratamientos del catálogo.
* Usuarios.

Las entidades transaccionales o históricas, como:

* Citas.
* Tratamientos realizados.
* Pagos.
* Registros de auditoría.
* Consultas clínicas.

deberán conservarse para mantener la trazabilidad del sistema.

---

# 17. Navegación general

La navegación dependerá de los permisos del usuario.

Una estructura inicial podría ser:

```text
Dashboard

Gestión
├── Pacientes
├── Odontólogos
├── Citas
├── Tratamientos
└── Pagos

Clínica
├── Historia Clínica
└── Odontograma

Administración
├── Usuarios
├── Roles y Permisos
└── Auditoría

Reportes
└── Reportes
```

Los elementos de navegación que el usuario no tenga permiso para utilizar no deberán mostrarse.

Sin embargo, la ocultación visual de opciones no sustituirá la validación de autorización en la aplicación.

---

# 18. Principales flujos funcionales

Los módulos estarán relacionados principalmente mediante los siguientes flujos:

### Flujo de atención

```text
Paciente
   ↓
Cita
   ↓
Atención
   ↓
Historia Clínica
   ↓
Odontograma
   ↓
Tratamiento
   ↓
Pago
```

### Flujo administrativo

```text
Usuario
   ↓
Rol
   ↓
Permisos
   ↓
Acceso a módulos
   ↓
Operaciones auditadas
```

### Flujo de tratamiento

```text
Catálogo de tratamiento
          ↓
Tratamiento realizado
          ↓
Paciente
          ↓
Pieza dental (si aplica)
          ↓
Pagos
```

Estos flujos servirán como base para definir posteriormente los casos de uso y las reglas de negocio.

---

# 19. Criterios generales para la implementación

Las funcionalidades descritas en este documento representan el alcance funcional inicial.

Durante la implementación:

* No deberán agregarse módulos fuera del alcance sin documentarlos previamente.
* Las reglas de negocio deberán implementarse fuera de la interfaz cuando corresponda.
* Las operaciones sensibles deberán estar protegidas mediante autorización.
* Las operaciones relevantes deberán generar registros de auditoría.
* La información histórica deberá conservarse.
* La interfaz deberá priorizar funcionalidad y claridad sobre complejidad visual.
* Las funcionalidades deberán mantenerse suficientemente simples para el contexto académico del proyecto.

Cualquier modificación importante del alcance deberá reflejarse en este documento antes de implementarse.
