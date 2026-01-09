namespace ClipBeam.Application.Abstractions.Pairing
{
    public interface IPairingEndpointProvider
    {
        (string host, int port) GetEndpoint();
    }
}