using Microsoft.IdentityModel.Tokens;
using Minio;
using Polly;
using Polly.Retry;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Simmakers.Interview.Areas.Identity.Services
{
    /// <summary>
    /// Service for managing blobs, spread over scopes.
    /// </summary>
    public class ScopedFileManager
    {
        private const string DeniedScopeCharacterReplacement = "_";

        private static readonly Regex DeniedScopeCharacterRegex =
            new Regex(@"[.\\/]", RegexOptions.Compiled);

        private static readonly AsyncRetryPolicy FileLoadRetryPolicy = Policy
            .Handle<FileLoadErrorException>()
            .RetryAsync(3);

        private static readonly string BucketName = nameof(ScopedFileManager).ToLowerInvariant();

        private readonly IBucketOperations _bucketOperations;
        private readonly IObjectOperations _objectOperations;

        private bool _isInitialized = false;

        public ScopedFileManager(IBucketOperations bucketOperations, IObjectOperations objectOperations)
            => (_bucketOperations, _objectOperations) = (bucketOperations, objectOperations);

        /// <summary>
        /// Stores a file
        /// </summary>
        /// <param name="scope">Scope to store the file within</param>
        /// <param name="name">Name of the file</param>
        /// <param name="contentType">MIME type of the file</param>
        /// <param name="uploadStream">Stream to read the file from</param>
        /// <param name="fileSize">Size of the file in bytes</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        public async Task StoreFileAsync(string scope, string name, string contentType, Stream uploadStream, long fileSize, CancellationToken cancellationToken = default)
        {
            await EnsureInitialized();

            await _objectOperations.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(BucketName)
                    // FIXME: VULNERABILITY: user input must be escaped
                    .WithObject($"{scope}/{name}")
                    .WithObjectSize(fileSize)
                    .WithStreamData(uploadStream)
                    .WithContentType(contentType),
                cancellationToken
            );
        }

        /// <summary>
        /// Loads a file
        /// </summary>
        /// <param name="scope">Scope the file was stored within</param>
        /// <param name="name">Name of the file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        public async Task<(string ContentType, byte[] Content)> LoadFileAsync(string scope, string name, CancellationToken cancellationToken = default)
        {
            await EnsureInitialized();

            var stat = await _objectOperations.StatObjectAsync(
                new StatObjectArgs()
                    .WithBucket(BucketName)
                    // FIXME: VULNERABILITY: user input must be escaped
                    .WithObject($"{scope}/{name}"),
                cancellationToken
            );


            var content = await FileLoadRetryPolicy
                .ExecuteAsync(async () => await LoadFileUnprotectedAsync(stat.ObjectName, stat.Size, cancellationToken));

            return (stat.ContentType, content);
        }

        private async Task<byte[]?> LoadFileUnprotectedAsync(string path, long size, CancellationToken cancellationToken = default)
        {
            // FIXME: this logic must be isolated on a consumer-producer stream abstraction level
            // This is required due to Minio API restrictions (callback instead of an output stream)
            using var loadResetEvent = new AutoResetEvent(false);

            byte[] content = new byte[size];
            bool loaded = false;

            await _objectOperations.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(BucketName)
                    // FIXME: VULNERABILITY: user input must be escaped
                    .WithObject(path)
                    .WithCallbackStream(async s =>
                    {
                        try
                        {
                            s.ReadExactly(content);
                            loaded = true;
                        }
                        catch (ObjectDisposedException)
                        {
                            // Minio connection may fail. Retry logic is present in LoadFileAsync - the parent method.
                        }
                        finally
                        {
                            loadResetEvent.Set();
                        }
                    }),
                cancellationToken
            );

            loadResetEvent.WaitOne();

            if (loaded)
            {
                return content;
            }

            throw new FileLoadErrorException();
        }

        /// <summary>
        /// Removes a scope and any files stored in it
        /// </summary>
        /// <param name="scope">Target scope</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        public async Task RemoveScopeAsync(string scope, CancellationToken cancellationToken = default)
        {
            await EnsureInitialized();

            var existingObjects = await _bucketOperations.ListObjectsAsync(
                new ListObjectsArgs()
                    .WithBucket(BucketName)
                    // FIXME: VULNERABILITY: user input must be escaped
                    .WithPrefix(scope),
                cancellationToken
            ).ToList();

            var objectNames = existingObjects
                .Select(it => it.Key)
                .ToList();

            await _objectOperations.RemoveObjectsAsync(
                new RemoveObjectsArgs()
                    .WithBucket(BucketName)
                    .WithObjects(objectNames),
                cancellationToken
            );
        }


        /// <summary>
        /// Deletes a file
        /// </summary>
        /// <param name="scope">Scope the file was stored within</param>
        /// <param name="name">File name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        public async Task DeleteFileAsync(string scope, string name, CancellationToken cancellationToken = default)
        {
            await EnsureInitialized();

            await _objectOperations.RemoveObjectAsync(
                new RemoveObjectArgs()
                    .WithBucket(BucketName)
                    // FIXME: VULNERABILITY: user input must be escaped
                    .WithObject($"{scope}/{name}"),
                cancellationToken
            );
        }

        /// <summary>
        /// Lists files stored within a scope
        /// </summary>
        /// <param name="scope">Target scope</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> ListScopeAsync(string scope, CancellationToken cancellationToken = default)
        {
            await EnsureInitialized();

            var existingObjects = await _bucketOperations.ListObjectsAsync(
                new ListObjectsArgs()
                    .WithBucket(BucketName)
                    .WithRecursive(true)
                    // FIXME: VULNERABILITY: user input must be escaped
                    .WithPrefix(scope),
                cancellationToken
            ).ToList();

            return existingObjects
                .Select(it => it.Key.Split('/').Last())
                .Where(it => !it.IsNullOrEmpty());
        }

        private async Task EnsureInitialized(CancellationToken cancellationToken = default)
        {
            if (_isInitialized)
            {
                return;
            }

            var bucketExists = await _bucketOperations.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(BucketName),
                cancellationToken
            );

            if (bucketExists)
            {
                _isInitialized = true;
                return;
            }

            await _bucketOperations.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(BucketName),
                cancellationToken
            );

            _isInitialized = true;
        }
    }
}
