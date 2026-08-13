# 🏠 InmoDev - Sistema de Gestión de Alquileres Temporales 🏠

> InmoDev es un sistema integral de gestión para agencias inmobiliarias especializadas en alquileres temporales de propiedades. La plataforma digitaliza y optimiza todos los procesos operativos de la agencia, desde el registro de propiedades hasta la gestión completa de reservas y pagos.

---

## 👥 Integrantes del Grupo

* **Juan Esteban Carreras** - *carrerasjuanesteban@gmail.com* - [@usuario_github](https://github.com/CarrerasJuan) 
* **Federico Galan** - *federico.galan2023@gmail.com* - [@usuario_github](https://github.com/Federico-Galan) 


---

## 📐 Modelado de Datos

A continuación se presenta el esquema del modelo de datos correspondiente a la aplicación:

### Diagrama Entidad-Relación (DER) / Diagrama de Clases

![Diagrama del Proyecto](./Diagrama/diagrama.png)

<details>
<summary>Ver diagrama en código Mermaid </summary>

```mermaid
erDiagram
    TiposInmueble {
        int Id PK
        varchar Nombre
        varchar Descripcion
        bool Activo
    }
    
    Propietarios {
        int Id PK
        varchar Nombre
        varchar Telefono
        varchar Email
        varchar Direccion
        bool Activo
        datetime FechaRegistro
    }
    
    Inquilinos {
        int Id PK
        varchar DNI
        varchar NombreCompleto
        varchar Telefono
        varchar Email
        varchar Direccion
        datetime FechaRegistro
    }
    
    Usuarios {
        int Id PK
        varchar Email
        varchar PasswordHash
        varchar Rol
        varchar Nombre
        varchar Avatar
        bool Activo
        datetime FechaCreacion
    }
    
    Inmueble {
        int Id PK
        int PropietarioId FK
        int TipoId FK
        varchar Direccion
        int CupoMaximo
        varchar Coordenadas
        decimal PrecioPorDia
        varchar ImagenPortada
        bool Disponible
        datetime FechaRegistro
    }
    
    Imagenes {
        int Id PK
        int InmuebleId FK
        varchar Url
        bool EsPortada
        int Orden
        datetime FechaRegistro
    }
    
    Reservas {
        int Id PK
        int InquilinoId FK
        int InmuebleId FK
        date FechaInicio
        date FechaFin
        date FechaFinReal
        decimal MontoPorDia
        decimal PorcentajeSeña
        varchar Estado
        datetime FechaCreacion
        int UsuarioCreadoId FK
        int UsuarioFinalizaId FK
        datetime FechaFinalizacion
        decimal MultaAplicada
    }
    
    Pagos {
        int Id PK
        int ReservaId FK
        varchar Concepto
        datetime FechaPago
        decimal Importe
        varchar Estado
        datetime FechaCreacion
        int UsuarioCreaId FK
        int UsuarioAnulaId FK
        datetime FechaAnulacion
    }
    
    Propietarios ||--o{ Inmueble : "posee"
    TiposInmueble ||--o{ Inmueble : "clasifica"
    Inmueble ||--o{ Imagenes : "tiene"
    Inquilinos ||--o{ Reservas : "realiza"
    Inmueble ||--o{ Reservas : "recibe"
    Usuarios ||--o{ Reservas : "crea_finaliza"
    Reservas ||--o{ Pagos : "genera"
    Usuarios ||--o{ Pagos : "registra_anula"
```

