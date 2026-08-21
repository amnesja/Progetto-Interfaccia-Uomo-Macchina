using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Composite;
using Microsoft.Extensions.Primitives;

namespace Ordo.Web.Infrastructure
{
    public class CustomCompositeFileProvider : IFileProvider
    {
        private readonly IFileProvider[] _fileProviders;

        public CustomCompositeFileProvider(params IFileProvider[] fileProviders)
        {
            _fileProviders = fileProviders ?? new IFileProvider[0];
        }

        public CustomCompositeFileProvider(IEnumerable<IFileProvider> fileProviders)
        {
            if (fileProviders == null)
            {
                throw new ArgumentNullException(nameof(fileProviders));
            }
            _fileProviders = fileProviders.ToArray();
        }

        /// <summary>
        /// Locates a file at the given path.
        /// </summary>
        /// <param name="subpath">The path that identifies the file. </param>
        /// <returns>The file information. Caller must check Exists property. This will be the first existing <see cref="IFileInfo"/> returned by the provided <see cref="IFileProvider"/> or a not found <see cref="IFileInfo"/> if no existing files is found.</returns>
        public IFileInfo GetFileInfo(string subpath)
        {
            foreach (var fileProvider in _fileProviders)
            {
                var fileInfo = fileProvider.GetFileInfo(subpath);
                if (fileInfo != null && fileInfo.Exists)
                {
                    return fileInfo;
                }
            }
            return new NotFoundFileInfo(subpath);
        }

        /// <summary>
        /// Enumerate a directory at the given path, if any.
        /// </summary>
        /// <param name="subpath">The path that identifies the directory</param>
        /// <returns>Contents of the directory. Caller must check Exists property.
        /// The content is a merge of the contents of the provided <see cref="IFileProvider"/>.
        /// When there is multiple <see cref="IFileInfo"/> with the same Name property, only the first one is included on the results.</returns>
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            var directoryContents = new CompositeDirectoryContents(_fileProviders, subpath);
            return directoryContents;
        }

        /// <summary>
        /// Creates a <see cref="IChangeToken"/> for the specified <paramref name="pattern"/>.
        /// </summary>
        /// <param name="pattern">Filter string used to determine what files or folders to monitor. Example: **/*.cs, *.*, subFolder/**/*.cshtml.</param>
        /// <returns>An <see cref="IChangeToken"/> that is notified when a file matching <paramref name="pattern"/> is added, modified or deleted.
        /// The change token will be notified when one of the change token returned by the provided <see cref="IFileProvider"/> will be notified.</returns>
        public IChangeToken Watch(string pattern)
        {
            // Watch all file providers
            var changeTokens = new List<IChangeToken>();
            foreach (var fileProvider in _fileProviders)
            {
                var changeToken = fileProvider.Watch(pattern);
                if (changeToken != null)
                {
                    changeTokens.Add(changeToken);
                }
            }

            // There is no change token with active change callbacks
            if (changeTokens.Count == 0)
            {
                return NullChangeToken.Singleton;
            }

            return new CompositeChangeToken(changeTokens);
        }

        /// <summary>
        /// Gets the list of configured <see cref="IFileProvider" /> instances.
        /// </summary>
        public IEnumerable<IFileProvider> FileProviders => _fileProviders;
    }

    public class CompositePhysicalFileProvider : IFileProvider
    {
        private readonly PhysicalFileProvider _p;
        private readonly string _baseFolder;
        private readonly string _relativeFolder;

        public CompositePhysicalFileProvider(string baseFolder, string relativeFolder)
        {
            if (string.IsNullOrWhiteSpace(baseFolder))
                throw new ArgumentNullException(nameof(baseFolder));
            if (string.IsNullOrWhiteSpace(relativeFolder))
                throw new ArgumentNullException(nameof(relativeFolder));

            // normalizza: _relativeFolder con separator standard (usato per confronti con subpath)
            _relativeFolder = Path.DirectorySeparatorChar + relativeFolder.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            _baseFolder = baseFolder;

            // costruisci percorso fisico e normalizza
            var physicalPath = Path.GetFullPath(Path.Combine(baseFolder, relativeFolder));
            _p = new PhysicalFileProvider(physicalPath);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            if (string.IsNullOrEmpty(subpath))
            {
                return _p.GetDirectoryContents(string.Empty);
            }

            // normalizza subpath in termini di separator e rimuovi eventuale trailing query/hash
            var normalized = subpath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

            if (normalized.StartsWith(_relativeFolder, System.StringComparison.OrdinalIgnoreCase))
            {
                var relative = normalized.Substring(_relativeFolder.Length).TrimStart(Path.DirectorySeparatorChar);
                return _p.GetDirectoryContents(relative);
            }
            else
            {
                // se subpath non è relativo alla cartella gestita, chiedi contenuto relativo direttamente
                var trimmed = normalized.TrimStart(Path.DirectorySeparatorChar);
                return _p.GetDirectoryContents(trimmed);
            }
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            if (string.IsNullOrEmpty(subpath))
            {
                return _p.GetFileInfo(string.Empty);
            }

            var normalized = subpath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

            if (normalized.StartsWith(_relativeFolder, System.StringComparison.OrdinalIgnoreCase))
            {
                var relative = normalized.Substring(_relativeFolder.Length).TrimStart(Path.DirectorySeparatorChar);
                return _p.GetFileInfo(relative);
            }
            else
            {
                var trimmed = normalized.TrimStart(Path.DirectorySeparatorChar);
                return _p.GetFileInfo(trimmed);
            }
        }

        public IChangeToken Watch(string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                return _p.Watch(string.Empty);
            }

            var normalized = filter.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

            if (normalized.StartsWith(_relativeFolder, System.StringComparison.OrdinalIgnoreCase))
            {
                var relative = normalized.Substring(_relativeFolder.Length).TrimStart(Path.DirectorySeparatorChar);
                return _p.Watch(relative);
            }
            else
            {
                var trimmed = normalized.TrimStart(Path.DirectorySeparatorChar);
                return _p.Watch(trimmed);
            }
        }
    }
}
