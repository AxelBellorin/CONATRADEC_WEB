# Publicación de CONATRADEC.AdminWeb en MonsterASP

## 1. Reemplazar y agregar archivos

Reemplaza:

- `CONATRADEC.AdminWeb.csproj`
- `Program.cs`
- `appsettings.json`
- `appsettings.Development.json`

Agrega:

- `appsettings.Production.json`
- `.gitignore`

Después cierra Visual Studio y elimina localmente las carpetas:

- `.vs`
- `bin`
- `obj`

No elimines carpetas del código fuente.

## 2. Probar compilación local

Desde la carpeta del proyecto ejecuta:

```powershell
dotnet clean
dotnet restore
dotnet build --configuration Release
```

Luego inicia la API local y ejecuta la web desde Visual Studio.

## 3. Crear el sitio en MonsterASP

1. Crea un sitio independiente para la web.
2. Selecciona .NET 9.
3. Activa HTTPS.
4. En la configuración ASP.NET Core usa `InProcess`.
5. En Deploy activa WebDeploy.
6. Descarga el archivo `.publishSettings`.

## 4. Variables de entorno recomendadas

En:

`Websites > Manage website > Scripting > Environment Variables`

agrega:

```text
ASPNETCORE_ENVIRONMENT = Production
ApiSettings__BaseUrl = http://conatradecnic.runasp.net/
```

Cuando el certificado HTTPS del backend esté activo, cambia únicamente la segunda variable por:

```text
ApiSettings__BaseUrl = https://conatradecnic.runasp.net/
```

Después reinicia el sitio web.

## 5. Publicar desde Visual Studio

1. Clic derecho sobre `CONATRADEC.AdminWeb.csproj`.
2. Selecciona `Publicar`.
3. Selecciona `Importar perfil`.
4. Importa el `.publishSettings` descargado de MonsterASP.
5. Abre `Mostrar todas las configuraciones`.
6. Usa:
   - Configuración: `Release`
   - Framework: `net9.0`
   - Implementación: dependiente del marco
   - Runtime: portable
   - Eliminar archivos adicionales en el destino: activado para la primera publicación
7. Pulsa `Publicar`.

El SDK genera automáticamente el `web.config`. No copies el
`applicationhost.config` de la carpeta `.vs`.

## 6. Si aparece un error 500

En MonsterASP revisa:

`Websites > Manage > Logs`

También confirma:

- Runtime .NET 9.
- Hosting Model `InProcess`.
- Variable `ApiSettings__BaseUrl`.
- Que la API responda desde el servidor.
- Que todos los archivos publicados estén dentro de `/wwwroot`.
