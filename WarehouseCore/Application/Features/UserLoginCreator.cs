namespace Application.Features;
public class UserLoginCreator
{
    public string CreateLogin(Ulid Id, string firstName, string secondName)
        => WmsCallsignGenerator.Generate(firstName, secondName, Id.ToString());
}