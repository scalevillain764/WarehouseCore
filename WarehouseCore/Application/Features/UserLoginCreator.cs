namespace Application.Features;
public class UserLoginCreator
{
    public virtual string CreateLogin(Ulid Id, string firstName, string secondName)
        => WmsCallsignGenerator.Generate(firstName, secondName, Id.ToString());
}