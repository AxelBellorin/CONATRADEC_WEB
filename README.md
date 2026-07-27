# CONATRADEC.AdminWeb

Portal administrativo web para CONATRADEC.

## Qué incluye esta primera base

- Proyecto ASP.NET Core 9 con Blazor Web App.
- Login conectado a `POST api/auth/login`.
- Lectura del usuario, rol, procedencia y permisos devueltos por la API.
- Menú administrativo adaptable a escritorio y móvil.
- Dashboard preparado para `GET api/dashboard/resumen`.
- Mapa dinámico con Leaflet, OpenStreetMap y agrupación de marcadores.
- Mapa preparado para `GET api/mapa/terrenos`.
- Configuración separada para desarrollo y producción.
- Diseño basado en la identidad visual de CONATRADEC.
- Sin paquetes NuGet externos obligatorios.

## Importante antes de producción

La API actual devuelve una propiedad `token`, pero todavía puede venir nula. Antes
de publicar el portal administrativo definitivo se debe implementar JWT y proteger
los endpoints del backend con autorización.

La sesión inicial de este proyecto vive mientras la conexión de Blazor Server está
activa. Cuando se implemente JWT, también se agregará persistencia segura de sesión.

## Requisitos

1. Visual Studio 2022 actualizado.
2. Carga de trabajo **ASP.NET y desarrollo web** instalada.
3. SDK de .NET 9.
4. Backend de CONATRADEC ejecutándose.

## Cómo abrirlo

1. Descomprime el ZIP.
2. Abre `CONATRADEC.AdminWeb.csproj` con Visual Studio.
3. Espera que Visual Studio restaure el proyecto.
4. Revisa `appsettings.Development.json`.
5. Confirma que la URL local de la API sea correcta.
6. Presiona `F5`.

Por defecto, en desarrollo se utiliza:

```json
"BaseUrl": "https://localhost:7176/"
```

La aplicación MAUI actual también apunta a esa URL local.

## Cómo usar la API publicada

Edita `appsettings.json`:

```json
"ApiSettings": {
  "BaseUrl": "http://conatradecnic.runasp.net/"
}
```

Cuando tengas SSL definitivo, cambia a HTTPS.

## Estructura

```text
CONATRADEC.AdminWeb
├── Components
│   ├── Layout
│   ├── Pages
│   └── Shared
├── Models
├── Properties
├── Services
├── wwwroot
│   ├── css
│   └── js
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
└── CONATRADEC.AdminWeb.csproj
```

## Endpoints que faltan en el backend

### Dashboard

```http
GET api/dashboard/resumen
```

Respuesta prevista:

```json
{
  "totalTerrenos": 25,
  "totalAnalisis": 80,
  "usuariosActivos": 12,
  "totalDiagnosticos": 30,
  "analisisPorMes": [
    { "mes": "Feb", "cantidad": 8 },
    { "mes": "Mar", "cantidad": 11 }
  ]
}
```

### Mapa

```http
GET api/mapa/terrenos
```

Respuesta prevista:

```json
[
  {
    "terrenoId": 1,
    "codigo": "TER-MAT-0001",
    "nombre": "Finca El Porvenir",
    "productor": "Juan Pérez",
    "latitud": 12.9256,
    "longitud": -85.9175,
    "departamento": "Matagalpa",
    "municipio": "Matagalpa",
    "extensionManzanas": 8.5,
    "estado": "Normal",
    "ultimoPh": 5.8
  }
]
```

Hasta que estos endpoints existan, el dashboard muestra ceros y el mapa presenta
dos puntos marcados claramente como demostrativos.

## Publicación en MonsterASP.NET

La carpeta para publicar se genera desde Visual Studio:

1. Clic derecho sobre el proyecto.
2. **Publicar**.
3. Elegir **Carpeta**.
4. Seleccionar `Release`.
5. Publicar.
6. Subir el contenido generado al subdominio administrativo.

Configuración sugerida:

```text
admin.tudominio.com  -> CONATRADEC.AdminWeb
api.tudominio.com    -> CONATRADEC_API
```

No publiques aún con información sensible hasta implementar JWT y HTTPS.
