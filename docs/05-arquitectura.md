# 07. Arquitectura del Sistema

## 1. Propósito

El sistema utilizará una arquitectura por capas para separar las responsabilidades de presentación, lógica de aplicación, dominio y acceso a infraestructura.

El objetivo es mantener el proyecto organizado, facilitar el trabajo de los integrantes del equipo y permitir que la aplicación pueda evolucionar sin que los cambios en una parte del sistema afecten innecesariamente a las demás.

La arquitectura debe mantenerse acorde al alcance del proyecto. No se utilizarán microservicios ni una arquitectura distribuida, ya que el sistema será desarrollado como una aplicación monolítica modular.

---

## 2. Estilo arquitectónico

El sistema utilizará:

* Aplicación web monolítica.
* Arquitectura por capas.
* Separación de responsabilidades.
* Desarrollo basado en módulos funcionales.
* Entity Framework Core para persistencia.
* SQL Server como base de datos.
* ASP.NET Core Identity para autenticación y gestión de credenciales.
* Blazor para la capa de presentación.

La comunicación principal seguirá el siguiente flujo:

```text
Usuario
   ↓
Presentación (Blazor)
   ↓
Aplicación
   ↓
Dominio
   ↓
Infraestructura
   ↓
SQL Server
```

La dependencia debe dirigirse hacia las capas internas. La interfaz de usuario no debe acceder directamente a la base de datos.

---

## 3. Estructura general

La solución estará organizada de la siguiente manera:

```text
src/
├── ClinicaDental.Web/
├── ClinicaDental.Application/
├── ClinicaDental.Domain/
└── ClinicaDental.Infrastructure/
```

### 3.1. ClinicaDental.Web

Contiene la aplicación Blazor y representa la capa de presentación.

Responsabilidades:

* Componentes y páginas Blazor.
* Layout y navegación.
* Formularios.
* Tablas y filtros.
* Validaciones relacionadas con la interfaz.
* Manejo de estados visuales.
* Presentación de mensajes al usuario.
* Control de acceso visual según permisos.
* Consumo de los servicios de aplicación.

Esta capa **no debe contener reglas de negocio importantes ni consultas directas a Entity Framework Core**.

Ejemplo:

```text
Pages/
├── Pacientes/
├── Odontologos/
├── Citas/
├── HistoriaClinica/
├── Odontograma/
├── Tratamientos/
├── Pagos/
├── Usuarios/
├── Auditoria/
└── Reportes/
```

---

## 4. Capa de Aplicación

Proyecto:

```text
ClinicaDental.Application
```

Esta capa coordina los casos de uso del sistema.

Responsabilidades:

* Implementar casos de uso.
* Coordinar operaciones entre diferentes entidades.
* Validar reglas necesarias para ejecutar una operación.
* Coordinar autorización por permisos.
* Utilizar interfaces para acceder a infraestructura.
* Ejecutar operaciones de consulta y modificación.
* Crear registros de auditoría cuando corresponda.
* Transformar datos entre modelos de dominio y modelos utilizados por la presentación.

La capa de aplicación no debe encargarse de cómo se almacenan físicamente los datos.

### Ejemplo

Para registrar un pago:

```text
RegistrarPago
    ↓
Validar tratamiento
    ↓
Calcular saldo pendiente
    ↓
Validar que el monto no exceda el saldo
    ↓
Registrar pago
    ↓
Registrar auditoría
```

El componente Blazor solamente solicita la operación y presenta el resultado.

---

## 5. Capa de Dominio

Proyecto:

```text
ClinicaDental.Domain
```

Es la capa que representa las reglas y conceptos principales del negocio.

Contendrá:

* Entidades.
* Enumeraciones.
* Reglas de negocio.
* Interfaces de repositorios o servicios que deban ser abstraídos.
* Objetos relacionados directamente con el dominio.

Principales entidades:

```text
Paciente
Odontologo
Cita
HistoriaClinica
ConsultaClinica
PiezaDental
RegistroOdontograma
Tratamiento
TratamientoRealizado
Pago
Usuario
Rol
Permiso
RegistroAuditoria
```

Esta capa no debe depender de:

* Blazor.
* Entity Framework Core.
* SQL Server.
* Componentes visuales.
* Servicios específicos de infraestructura.

La lógica de dominio debe poder entenderse independientemente de la interfaz gráfica o de la base de datos.

---

## 6. Capa de Infraestructura

Proyecto:

```text
ClinicaDental.Infrastructure
```

Contiene los detalles técnicos necesarios para comunicarse con recursos externos.

Responsabilidades:

* Entity Framework Core.
* `DbContext`.
* Configuración de entidades.
* Migraciones.
* Implementación de repositorios.
* Consultas a SQL Server.
* ASP.NET Core Identity.
* Persistencia de usuarios, roles y permisos.
* Servicios técnicos externos, si posteriormente fueran necesarios.

Ejemplo:

```text
Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs
│   ├── Configurations/
│   └── Migrations/
├── Repositories/
├── Identity/
└── Services/
```

La infraestructura implementa las interfaces que necesita la capa de aplicación o dominio.

---

## 7. Dependencias entre proyectos

Las dependencias principales serán:

```text
ClinicaDental.Web
        ↓
ClinicaDental.Application
        ↓
ClinicaDental.Domain

ClinicaDental.Infrastructure
        ↓
ClinicaDental.Application
        ↓
ClinicaDental.Domain
```

La capa de dominio será la más independiente.

Una representación simplificada:

```text
                 ┌─────────────────────┐
                 │  ClinicaDental.Web  │
                 │      Blazor         │
                 └──────────┬──────────┘
                            │
                            ▼
                 ┌─────────────────────┐
                 │ Application         │
                 │ Casos de uso        │
                 └──────────┬──────────┘
                            │
                            ▼
                 ┌─────────────────────┐
                 │ Domain              │
                 │ Entidades y reglas  │
                 └─────────────────────┘
                            ▲
                            │
                 ┌──────────┴──────────┐
                 │ Infrastructure      │
                 │ EF Core / Identity  │
                 │ SQL Server          │
                 └─────────────────────┘
```

La aplicación no debe saltarse capas para acceder directamente a la base de datos.

---

## 8. Organización interna de los módulos

Los módulos funcionales definidos en `03-modulos-y-funcionalidades.md` deberán mantenerse reconocibles dentro de la solución.

Los principales módulos serán:

```text
Pacientes
Odontologos
Citas
HistoriaClinica
Odontograma
Tratamientos
Pagos
Usuarios
RolesPermisos
Auditoria
Reportes
```

La organización interna puede seguir una estructura orientada a funcionalidades.

Por ejemplo:

```text
Application/
├── Pacientes/
│   ├── CrearPaciente/
│   ├── EditarPaciente/
│   ├── ConsultarPacientes/
│   └── DesactivarPaciente/
│
├── Citas/
│   ├── CrearCita/
│   ├── EditarCita/
│   ├── CancelarCita/
│   └── ConsultarCitas/
│
├── Pagos/
│   ├── RegistrarPago/
│   └── ConsultarPagos/
│
└── ...
```

No es obligatorio que cada operación se convierta en una cantidad excesiva de clases. La organización debe mantenerse proporcional al tamaño y complejidad del proyecto.

---

## 9. Acceso a datos

Entity Framework Core será utilizado como ORM para acceder a SQL Server.

El flujo será:

```text
Componente Blazor
       ↓
Servicio / Caso de uso
       ↓
Repositorio o servicio de persistencia
       ↓
Entity Framework Core
       ↓
SQL Server
```

Los componentes de Blazor no deberán realizar operaciones como:

```csharp
_dbContext.Pacientes.ToListAsync();
```

directamente.

En su lugar, deberán solicitar la información mediante la capa de aplicación.

Ejemplo conceptual:

```text
PacientePage
    ↓
PacienteService
    ↓
IPacienteRepository
    ↓
PacienteRepository
    ↓
ApplicationDbContext
```

---

## 10. Entity Framework Core

Entity Framework Core será responsable de la persistencia de las entidades.

La configuración de las entidades deberá mantenerse separada cuando sea necesario para evitar colocar configuraciones extensas dentro del `DbContext`.

Ejemplo:

```text
Persistence/
├── ApplicationDbContext.cs
└── Configurations/
    ├── PacienteConfiguration.cs
    ├── OdontologoConfiguration.cs
    ├── CitaConfiguration.cs
    ├── HistoriaClinicaConfiguration.cs
    ├── TratamientoConfiguration.cs
    ├── PagoConfiguration.cs
    └── ...
```

Las configuraciones deberán definir, entre otros aspectos:

* Claves primarias.
* Relaciones.
* Claves foráneas.
* Restricciones de nulabilidad.
* Longitudes máximas.
* Índices.
* Restricciones de unicidad cuando correspondan.
* Precisión de valores monetarios.

---

## 11. Autenticación y autorización

La autenticación será manejada mediante ASP.NET Core Identity.

El sistema distinguirá entre:

```text
Autenticación
    ↓
¿Quién es el usuario?

Autorización
    ↓
¿Qué puede hacer ese usuario?
```

Los roles y permisos definidos en `02-actores-y-roles.md` serán utilizados para controlar el acceso.

Los permisos seguirán el formato:

```text
<Modulo>.<Accion>
```

Ejemplos:

```text
Pacientes.Ver
Pacientes.Crear
Pacientes.Editar
Citas.Crear
HistoriaClinica.Editar
Odontograma.Editar
Pagos.Crear
Usuarios.Editar
Auditoria.Ver
Reportes.Ver
```

El control de acceso tendrá dos niveles:

### Presentación

La interfaz puede ocultar opciones que el usuario no tenga permitido utilizar.

### Aplicación

La operación también deberá ser validada en la capa de aplicación.

Ocultar un botón no constituye una medida suficiente de autorización.

---

## 12. Auditoría

La auditoría será implementada como una preocupación transversal de la aplicación.

Las operaciones relevantes generarán un `RegistroAuditoria` con información como:

```text
Usuario
FechaHora
Accion
Modulo
Entidad
EntidadId
Descripcion
```

Ejemplos:

```text
Crear paciente
Editar paciente
Desactivar paciente
Crear cita
Cancelar cita
Modificar historia clínica
Modificar odontograma
Registrar tratamiento
Registrar pago
Modificar permisos
```

La auditoría no deberá alterar la operación principal ni convertirse en una dependencia directa de los componentes de interfaz.

La capa de aplicación será responsable de coordinar la generación de los registros de auditoría.

---

## 13. Manejo de errores

Los errores deberán manejarse de forma centralizada y controlada.

Se distinguirán principalmente:

* Errores de validación.
* Operaciones no autorizadas.
* Entidades inexistentes.
* Violaciones de reglas de negocio.
* Errores inesperados de infraestructura.

Los errores esperados deberán convertirse en mensajes comprensibles para el usuario.

Por ejemplo:

```text
No se puede registrar la cita:
el odontólogo ya tiene una cita activa
en el horario seleccionado.
```

No se deberá mostrar al usuario información técnica innecesaria, como excepciones completas de Entity Framework o detalles internos de SQL Server.

---

## 14. Validaciones

Las validaciones se distribuirán según su responsabilidad.

### Presentación

Validaciones relacionadas con la entrada del usuario:

* Campos obligatorios.
* Formato de correo.
* Formatos de fecha.
* Longitudes de texto.
* Formato de números.

### Aplicación / Dominio

Reglas relacionadas con el funcionamiento del negocio:

* No crear citas para pacientes inactivos.
* No crear citas para odontólogos inactivos.
* No permitir solapamiento de citas.
* No registrar pagos superiores al saldo pendiente.
* No seleccionar tratamientos inactivos.
* No permitir determinadas transiciones de estados.
* Mantener la consistencia entre paciente, cita y consulta clínica.

Las validaciones importantes no deben existir únicamente en la interfaz.

---

## 15. Transacciones

Las operaciones que modifiquen varias entidades relacionadas deberán ejecutarse dentro de una misma transacción cuando sea necesario garantizar consistencia.

Por ejemplo, una operación que:

```text
Registrar tratamiento
        +
Registrar auditoría
```

deberá evitar quedar parcialmente completada.

De igual manera, las operaciones que involucren múltiples modificaciones relacionadas deberán considerar la atomicidad de la operación.

No todas las consultas necesitan una transacción explícita.

---

## 16. Reportes

Los reportes no serán tratados como entidades persistentes del dominio.

Serán consultas construidas en la capa de aplicación utilizando la infraestructura de acceso a datos.

Ejemplos:

```text
Reporte de citas
Reporte de pacientes
Reporte de tratamientos
Reporte de pagos
```

El resultado de una consulta podrá utilizar modelos específicos para reportes, evitando exponer directamente las entidades de persistencia a la interfaz.

---

## 17. Modelos de transferencia

Cuando sea necesario, la capa de aplicación podrá utilizar DTOs o modelos específicos para transportar información entre capas.

Por ejemplo:

```text
PacienteDto
CrearPacienteRequest
EditarPacienteRequest
PacienteDetalleDto
CitaDto
RegistrarPagoRequest
ReportePagosDto
```

No es obligatorio crear un DTO para absolutamente cada clase del sistema. Se utilizarán cuando ayuden a controlar los datos que entran y salen de los casos de uso.

La interfaz no debe depender innecesariamente de las entidades de persistencia.

---

## 18. Seguridad

El sistema deberá aplicar como mínimo:

* Autenticación mediante ASP.NET Core Identity.
* Autorización mediante roles y permisos.
* Contraseñas almacenadas mediante los mecanismos de Identity.
* Validación de autorización en operaciones sensibles.
* Protección de las páginas y operaciones administrativas.
* Auditoría de operaciones relevantes.
* No exposición de credenciales ni secretos dentro del código fuente.
* Configuración de conexión a la base de datos mediante configuración segura.

La aplicación deberá seguir el principio de mínimo privilegio.

---

## 19. Configuración

Las configuraciones dependientes del entorno no deberán estar codificadas directamente en las clases.

Se utilizarán mecanismos de configuración de ASP.NET Core.

Ejemplos:

```text
Cadena de conexión
Configuración de Identity
Configuración de logging
Configuraciones específicas del entorno
```

Los secretos no deberán almacenarse en el repositorio.

---


## 21. Navegación y presentación

La navegación de la aplicación seguirá la estructura funcional definida previamente.

```text
Dashboard

Gestión
├── Pacientes
├── Odontólogos
└── Citas

Clínica
├── Historia Clínica
├── Odontograma
└── Tratamientos

Finanzas
└── Pagos

Administración
├── Usuarios
├── Roles y Permisos
└── Auditoría

Reportes
```

Las opciones visibles deberán depender de los permisos del usuario.

La interfaz deberá priorizar:

* Formularios claros.
* Tablas.
* Filtros.
* Modales cuando sean útiles.
* Navegación sencilla.
* Diseño responsive básico.

No se requiere una interfaz altamente visual ni animaciones complejas.

---

## 22. Principios de implementación

Durante el desarrollo deberán mantenerse los siguientes principios:

### Separación de responsabilidades

Cada capa debe encargarse de aquello que le corresponde.

### No duplicar reglas de negocio

Una regla importante no debe depender únicamente de un componente visual.

### Bajo acoplamiento

Los componentes no deberán depender directamente de implementaciones concretas de infraestructura cuando pueda utilizarse una abstracción adecuada.

### Simplicidad

No se agregarán patrones, librerías o capas que no aporten valor al alcance del proyecto.

### Consistencia

Los módulos deberán seguir convenciones similares de nombres, estructura y manejo de errores.

### Trazabilidad

Las operaciones importantes deberán poder relacionarse con el usuario que las realizó mediante auditoría.

### Evolución controlada

Las nuevas funcionalidades deberán incorporarse siguiendo la arquitectura existente en lugar de crear caminos alternativos dentro de la aplicación.

---

## 23. Tecnologías

La implementación inicial utilizará:

| Componente           | Tecnología                                 |
| -------------------- | ------------------------------------------ |
| Lenguaje             | C#                                         |
| Framework            | .NET / ASP.NET Core                        |
| UI                   | Blazor                                     |
| ORM                  | Entity Framework Core                      |
| Base de datos        | SQL Server                                 |
| Autenticación        | ASP.NET Core Identity                      |
| Frontend CSS         | Bootstrap o librería compatible con Blazor |
| Control de versiones | Git                                        |
| Arquitectura         | Monolito por capas                         |

La selección definitiva de la librería de componentes visuales podrá realizarse durante la implementación, siempre que sea compatible con Blazor y no introduzca complejidad innecesaria.

---
