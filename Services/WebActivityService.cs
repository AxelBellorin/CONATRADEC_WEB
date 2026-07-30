namespace CONATRADEC.AdminWeb.Services;

/// <summary>
/// Comparte la actividad humana detectada en el navegador con ApiClientService.
/// Las consultas automáticas no incrementan esta versión.
/// </summary>
public sealed class WebActivityService
{
    private long versionActividad;
    private long versionConfirmada;

    public void RegistrarActividad()
    {
        Interlocked.Increment(
            ref versionActividad);
    }

    public long ObtenerVersionPendiente()
    {
        long version =
            Interlocked.Read(
                ref versionActividad);

        long confirmada =
            Interlocked.Read(
                ref versionConfirmada);

        return version > confirmada
            ? version
            : 0;
    }

    public void Confirmar(long versionEnviada)
    {
        if (versionEnviada <= 0)
            return;

        while (true)
        {
            long actual =
                Interlocked.Read(
                    ref versionConfirmada);

            if (actual >= versionEnviada)
                return;

            if (Interlocked.CompareExchange(
                    ref versionConfirmada,
                    versionEnviada,
                    actual) == actual)
            {
                return;
            }
        }
    }

    public void Reiniciar()
    {
        Interlocked.Exchange(
            ref versionActividad,
            0);

        Interlocked.Exchange(
            ref versionConfirmada,
            0);
    }
}
