namespace AutoMarket.BuildingBlocks.Application;

// İstifadəçi id-si yalnız token-dəki sub-dan götürülür (SEC-AUTHZ-02)
public interface ICurrentUser
{
    public bool IsAuthenticated { get; }

    // Autentifikasiya olunmamış sorğuda çağırılması proqramçı xətasıdır
    public Guid Id { get; }
}
