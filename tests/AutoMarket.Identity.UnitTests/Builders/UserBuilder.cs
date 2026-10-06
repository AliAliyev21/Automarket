using AutoMarket.Identity.Domain.Users;

namespace AutoMarket.Identity.UnitTests.Builders;

// Etibarlı default dəyərli istifadəçi; test yalnız onun üçün vacib olanı dəyişir (CONVENTIONS §11.4)
internal sealed class UserBuilder
{
    private Guid _id = Guid.CreateVersion7(TestData.Now);
    private string _email = "user@automarket.az";
    private string _name = "Əli Məmmədov";
    private bool _confirmed = true;

    public UserBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UserBuilder Unconfirmed()
    {
        _confirmed = false;
        return this;
    }

    public User Build()
    {
        var user = User.Register(_id, _email, _name, phone: null, TestData.Now.AddDays(-1));
        user.PasswordHash = "hash";

        if (_confirmed)
        {
            user.ConfirmEmail(TestData.Now.AddDays(-1));
        }

        user.ClearDomainEvents();
        return user;
    }
}
