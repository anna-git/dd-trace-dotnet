using System;
using Microsoft.ServiceFabric.Services.Remoting.V2;

namespace Datadog.Trace.ServiceFabric
{
#pragma warning disable SA1649 // File name must match first type name
    internal interface IServiceRemotingRequestEventArgs
    {
        public IServiceRemotingRequestMessage? Request { get; }

        public Uri? ServiceUri { get; }

        public string? MethodName { get; }
    }
#pragma warning restore SA1649 // File name must match first type name

    internal interface IServiceRemotingRequestMessageHeader
    {
        int MethodId { get; set; }

        int InterfaceId { get; set; }

        string? InvocationId { get; set; }

        string? MethodName { get; set; }

        void AddHeader(string headerName, byte[] headerValue);

#pragma warning disable SA1011 // Closing square brackets must be spaced correctly
        bool TryGetHeaderValue(string headerName, out byte[]? headerValue);
#pragma warning restore SA1011 // Closing square brackets must be spaced correctly
    }

    internal interface IServiceRemotingResponseEventArgs
    {
        public IServiceRemotingResponseMessage Response { get; }

        public IServiceRemotingRequestMessage Request { get; }
    }

    internal interface IServiceRemotingFailedResponseEventArgs
    {
        public Exception? Error { get; }

        public IServiceRemotingRequestMessage Request { get; }
    }

    internal interface IServiceRemotingResponseMessage
    {
        IServiceRemotingResponseMessageHeader GetHeader();

        IServiceRemotingResponseMessageBody GetBody();
    }

    internal interface IServiceRemotingRequestMessage
    {
        IServiceRemotingRequestMessageHeader GetHeader();

        IServiceRemotingRequestMessageBody GetBody();
    }

    internal interface IServiceRemotingResponseMessageHeader
    {
        void AddHeader(string headerName, byte[] headerValue);

        bool TryGetHeaderValue(string headerName, out byte[] headerValue);

        bool CheckIfItsEmpty();
    }
}
