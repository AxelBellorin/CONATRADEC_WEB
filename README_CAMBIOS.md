# README_CAMBIOS.md

## CONATRADEC WEB - Simplificación de términos técnicos del frontend

Este paquete contiene únicamente los archivos del proyecto WEB que fueron modificados.
No incluye el proyecto completo, archivos de compilación, cachés, secretos ni archivos sin cambios.

### Objetivo
Simplificar la terminología técnica visible para los usuarios del portal CONATRADEC sin modificar la lógica de negocio, contratos de la API, rutas, modelos, autenticación ni base de datos.

### Alcance
- Proyecto afectado: CONATRADEC_WEB.
- Cambios: solo frontend WEB y mensajes generados por servicios del propio frontend que pueden mostrarse al usuario.
- Archivos de código modificados: 39.
- Backend/API: sin cambios.
- Escritorio/Android: sin cambios.
- Base de datos: sin cambios.

### Cambios principales
- `Dashboard ejecutivo` se muestra como `Resumen ejecutivo`.
- Se eliminó el indicador visible `Datos de la API`.
- `API`, `backend`, `servidor`, `endpoint`, `HTTP` y `token` se sustituyeron en mensajes de usuario por redacciones comprensibles.
- `Offline` y `Sincronización offline` se presentan como `Sin conexión` o `Registro realizado sin conexión`.
- `Último latido` se presenta como `Última conexión`.
- `Build` se presenta como `Versión interna`.
- `Llaves de descarga` se presenta como `Códigos de descarga`.
- Se retiraron de la interfaz datos técnicos como SHA-256, endpoint y código HTTP cuando no aportan valor al usuario.
- Los identificadores técnicos internos, rutas `api/...`, propiedades, clases y contratos se conservaron para no afectar el funcionamiento.

### Archivos modificados
- `Components/Layout/MainLayout.razor`
- `Components/Pages/Actualizaciones.razor`
- `Components/Pages/AlertasAgricolas.razor`
- `Components/Pages/AuditoriaAnalisis.razor`
- `Components/Pages/Bitacora.razor`
- `Components/Pages/ClimaLabores.razor`
- `Components/Pages/Configuracion.razor`
- `Components/Pages/ConfiguracionAlertas.razor`
- `Components/Pages/ControlAnalisis.razor`
- `Components/Pages/ControlFitosanitario.razor`
- `Components/Pages/DescargaProtegida.razor`
- `Components/Pages/DispositivosConectados.razor`
- `Components/Pages/ExtraccionNutrientes.razor`
- `Components/Pages/FuentesNutrientes.razor`
- `Components/Pages/Home.razor`
- `Components/Pages/InspeccionFitosanitaria.razor`
- `Components/Pages/LlavesDescarga.razor`
- `Components/Pages/MapaRelacionesAnalisis.razor`
- `Components/Pages/PortalCentroGeoespacial.razor`
- `Components/Pages/PortalPropietario.razor`
- `Components/Pages/Publicaciones.razor`
- `Components/Pages/RangosNutricionales.razor`
- `Components/Pages/ReglasClima.razor`
- `Components/Pages/Reportes.razor`
- `Components/Pages/SeguimientoAlertas.razor`
- `Components/Pages/Terrenos.razor`
- `Components/Pages/UnidadesConversiones.razor`
- `Components/Pages/Usuarios.razor`
- `Components/Shared/InspeccionDiagnosticosVisuales.razor`
- `Components/Shared/PronosticoTerrenoPanel.razor`
- `Services/ActualizacionesService.cs`
- `Services/AuthStateService.cs`
- `Services/ControlAnalisisService.cs`
- `Services/DescargasPublicasService.cs`
- `Services/InspeccionFitosanitariaWebService.cs`
- `Services/LlavesDescargaService.cs`
- `Services/UsuarioService.cs`
- `wwwroot/js/dispositivosMapa.js`
- `wwwroot/js/mapaRelacionesAnalisis.js`

### Integración
1. Realiza una copia de seguridad de tu proyecto WEB.
2. Descomprime este ZIP.
3. Copia `Components`, `Services` y `wwwroot` sobre la raíz de `CONATRADEC_WEB`.
4. Permite reemplazar únicamente los archivos incluidos en este paquete.
5. No elimines otros archivos del proyecto.
6. Compila y prueba el proyecto antes de publicarlo.

### Validaciones realizadas
- Se verificaron los 39 archivos fuente contra los hashes de la rama pública `master` actual del repositorio.
- Se confirmó que los archivos base utilizados coinciden exactamente con los archivos actuales de `master`.
- Se realizó una revisión posterior para comprobar que las frases técnicas objetivo ya no permanezcan visibles al usuario.
- Se ejecutó `git diff --check`; no se detectaron errores de espacios en blanco introducidos por los cambios.

### Compilación
No se ejecutó `dotnet build` porque el entorno disponible no tiene instalado el SDK de .NET.
Por este motivo, la compilación final debe realizarse en el entorno de desarrollo de CONATRADEC antes de publicar.

### Nota importante
Los nombres internos como `ApiClientService`, `Token`, `Endpoint`, `BuildApp`, `Offline`, `LlaveDescarga` y las rutas `api/...` se mantienen cuando forman parte del código, modelos o contratos. Cambiarlos habría podido romper compatibilidad con el resto del sistema.
