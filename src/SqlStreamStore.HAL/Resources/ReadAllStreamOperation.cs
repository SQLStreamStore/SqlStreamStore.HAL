namespace SqlStreamStore.HAL.Resources
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using SqlStreamStore.Streams;

    internal class ReadAllStreamOperation : IStreamStoreOperation<ReadAllPage>
    {
        private readonly long _fromPositionInclusive;
        private readonly int _maxCount;

        public ReadAllStreamOperation(HttpRequest request)
        {
            EmbedPayload = request.Query.TryGetValue("e", out _);

            ReadDirection = request.Query["d"] == "f"
                ? Constants.ReadDirection.Forwards
                : Constants.ReadDirection.Backwards;

            if(!long.TryParse(request.Query["p"], out _fromPositionInclusive))
            {
                _fromPositionInclusive = ReadDirection > 0 ? Position.Start : Position.End;
            }

            if(!int.TryParse(request.Query["m"], out _maxCount))
            {
                _maxCount = Constants.MaxCount;
            }

            Self = ReadDirection == Constants.ReadDirection.Forwards
                ? LinkFormatter.FormatForwardLink(
                    Constants.Streams.All,
                    MaxCount,
                    FromPositionInclusive,
                    EmbedPayload)
                : LinkFormatter.FormatBackwardLink(
                    Constants.Streams.All,
                    MaxCount,
                    FromPositionInclusive,
                    EmbedPayload);

            IsUriCanonical = Self.Remove(0, Constants.Streams.All.Length)
                             == request.QueryString.ToUriComponent();
        }

        public long FromPositionInclusive => _fromPositionInclusive;
        public int MaxCount => _maxCount;
        public bool EmbedPayload { get; }
        public int ReadDirection { get; }
        public string Self { get; }
        public bool IsUriCanonical { get; }

        public Task<ReadAllPage> Invoke(IStreamStore streamStore, CancellationToken ct)
            => ReadDirection == Constants.ReadDirection.Forwards
                ? streamStore.ReadAllForwards(_fromPositionInclusive, _maxCount, EmbedPayload, ct)
                : streamStore.ReadAllBackwards(_fromPositionInclusive, _maxCount, EmbedPayload, ct);
    }
}