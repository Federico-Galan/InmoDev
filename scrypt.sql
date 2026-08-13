CREATE DATABASE IF NOT EXISTS SistemaInmobiliario
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE SistemaInmobiliario;

-- =============================================
-- TIPOS DE INMUEBLE
-- =============================================
CREATE TABLE TiposInmueble (
    Id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Descripcion VARCHAR(255) NULL,
    Activo BOOLEAN NOT NULL DEFAULT TRUE,
    UNIQUE KEY UQ_TiposInmueble_Nombre (Nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- PROPIETARIOS
-- =============================================
CREATE TABLE Propietarios (
    Id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Telefono VARCHAR(50) NULL,
    Email VARCHAR(150) NULL,
    Direccion VARCHAR(255) NULL,
    Activo BOOLEAN NOT NULL DEFAULT TRUE,
    FechaRegistro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY UQ_Propietarios_Email (Email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- INQUILINOS
-- =============================================
CREATE TABLE Inquilinos (
    Id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    DNI VARCHAR(20) NOT NULL,
    NombreCompleto VARCHAR(150) NOT NULL,
    Telefono VARCHAR(50) NULL,
    Email VARCHAR(150) NULL,
    Direccion VARCHAR(255) NULL,
    FechaRegistro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY UQ_Inquilinos_DNI (DNI),
    UNIQUE KEY UQ_Inquilinos_Email (Email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- USUARIOS
-- =============================================
CREATE TABLE Usuarios (
    Id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    Email VARCHAR(150) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Rol VARCHAR(50) NOT NULL,
    Nombre VARCHAR(100) NOT NULL,
    Avatar VARCHAR(255) NULL,
    Activo BOOLEAN NOT NULL DEFAULT TRUE,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY UQ_Usuarios_Email (Email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- INMUEBLES
-- =============================================
CREATE TABLE Inmueble (
    Id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    PropietarioId INT UNSIGNED NOT NULL,
    TipoId INT UNSIGNED NOT NULL,
    Direccion VARCHAR(255) NOT NULL,
    CupoMaximo INT UNSIGNED NULL,
    Coordenadas VARCHAR(255) NULL,
    PrecioPorDia DECIMAL(12,2) NOT NULL,
    ImagenPortada VARCHAR(255) NULL,
    Disponible BOOLEAN NOT NULL DEFAULT TRUE,
    FechaRegistro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    KEY IX_Inmueble_PropietarioId (PropietarioId),
    KEY IX_Inmueble_TipoId (TipoId),
    CONSTRAINT CHK_Inmueble_CupoMaximo CHECK (CupoMaximo IS NULL OR CupoMaximo > 0),
    CONSTRAINT CHK_Inmueble_PrecioPorDia CHECK (PrecioPorDia >= 0),
    CONSTRAINT FK_Inmueble_Propietario
        FOREIGN KEY (PropietarioId)
        REFERENCES Propietarios(Id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT FK_Inmueble_Tipo
        FOREIGN KEY (TipoId)
        REFERENCES TiposInmueble(Id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- IMAGENES
-- =============================================
CREATE TABLE Imagenes (
    Id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    InmuebleId INT UNSIGNED NOT NULL,
    Url VARCHAR(500) NOT NULL,
    EsPortada BOOLEAN NOT NULL DEFAULT FALSE,
    Orden INT UNSIGNED NULL,
    FechaRegistro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    KEY IX_Imagenes_InmuebleId (InmuebleId),
    CONSTRAINT CHK_Imagenes_Orden CHECK (Orden IS NULL OR Orden >= 0),
    CONSTRAINT FK_Imagenes_Inmueble
        FOREIGN KEY (InmuebleId)
        REFERENCES Inmueble(Id)
        ON UPDATE CASCADE
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- RESERVAS
-- =============================================
CREATE TABLE Reservas (
    Id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    InquilinoId INT UNSIGNED NOT NULL,
    InmuebleId INT UNSIGNED NOT NULL,
    FechaInicio DATE NOT NULL,
    FechaFin DATE NOT NULL,
    FechaFinReal DATE NULL,
    MontoPorDia DECIMAL(12,2) NOT NULL,
    PorcentajeSena DECIMAL(5,2) NULL,
    Estado VARCHAR(50) NOT NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UsuarioCreadoId INT UNSIGNED NOT NULL,
    UsuarioFinalizaId INT UNSIGNED NULL,
    FechaFinalizacion DATETIME NULL,
    MultaAplicada DECIMAL(12,2) NULL,
    KEY IX_Reservas_InquilinoId (InquilinoId),
    KEY IX_Reservas_InmuebleId (InmuebleId),
    KEY IX_Reservas_UsuarioCreadoId (UsuarioCreadoId),
    KEY IX_Reservas_UsuarioFinalizaId (UsuarioFinalizaId),
    KEY IX_Reservas_Estado (Estado),
    CONSTRAINT CHK_Reservas_Fechas CHECK (FechaFin >= FechaInicio),
    CONSTRAINT CHK_Reservas_FechaFinReal CHECK (FechaFinReal IS NULL OR FechaFinReal >= FechaInicio),
    CONSTRAINT CHK_Reservas_MontoPorDia CHECK (MontoPorDia >= 0),
    CONSTRAINT CHK_Reservas_PorcentajeSena CHECK (PorcentajeSena IS NULL OR (PorcentajeSena >= 0 AND PorcentajeSena <= 100)),
    CONSTRAINT CHK_Reservas_MultaAplicada CHECK (MultaAplicada IS NULL OR MultaAplicada >= 0),
    CONSTRAINT FK_Reservas_Inquilino
        FOREIGN KEY (InquilinoId)
        REFERENCES Inquilinos(Id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT FK_Reservas_Inmueble
        FOREIGN KEY (InmuebleId)
        REFERENCES Inmueble(Id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT FK_Reservas_UsuarioCreado
        FOREIGN KEY (UsuarioCreadoId)
        REFERENCES Usuarios(Id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT FK_Reservas_UsuarioFinaliza
        FOREIGN KEY (UsuarioFinalizaId)
        REFERENCES Usuarios(Id)
        ON UPDATE CASCADE
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- PAGOS
-- =============================================
CREATE TABLE Pagos (
    Id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    ReservaId INT UNSIGNED NOT NULL,
    Concepto VARCHAR(255) NULL,
    FechaPago DATETIME NOT NULL,
    Importe DECIMAL(12,2) NOT NULL,
    Estado VARCHAR(50) NOT NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UsuarioCreaId INT UNSIGNED NOT NULL,
    UsuarioAnulaId INT UNSIGNED NULL,
    FechaAnulacion DATETIME NULL,
    KEY IX_Pagos_ReservaId (ReservaId),
    KEY IX_Pagos_UsuarioCreaId (UsuarioCreaId),
    KEY IX_Pagos_UsuarioAnulaId (UsuarioAnulaId),
    KEY IX_Pagos_Estado (Estado),
    CONSTRAINT CHK_Pagos_Importe CHECK (Importe >= 0),
    CONSTRAINT CHK_Pagos_FechaAnulacion CHECK (FechaAnulacion IS NULL OR FechaAnulacion >= FechaPago),
    CONSTRAINT FK_Pagos_Reserva
        FOREIGN KEY (ReservaId)
        REFERENCES Reservas(Id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT FK_Pagos_UsuarioCrea
        FOREIGN KEY (UsuarioCreaId)
        REFERENCES Usuarios(Id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT FK_Pagos_UsuarioAnula
        FOREIGN KEY (UsuarioAnulaId)
        REFERENCES Usuarios(Id)
        ON UPDATE CASCADE
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
