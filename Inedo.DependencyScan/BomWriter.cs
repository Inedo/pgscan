using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Inedo.DependencyScan
{
    /// <summary>
    /// Generates a minimal SBOM file.
    /// </summary>
    public class BomWriter : IDisposable
    {
        private readonly XmlWriter writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BomWriter"/> class.
        /// </summary>
        /// <param name="stream">Stream into which SBOM is written.</param>
        public BomWriter(Stream stream)
        {
            this.writer = XmlWriter.Create(new StreamWriter(stream, new UTF8Encoding(false)), new XmlWriterSettings { CloseOutput = true, WriteEndDocumentOnClose = true });
            this.writer.WriteStartElement("bom");
            this.writer.WriteAttributeString("serialNumber", "urn:uuid:" + Guid.NewGuid().ToString("n"));
            this.writer.WriteAttributeString("version", "1");
        }

        /// <summary>
        /// Records project information in the SBOM.
        /// </summary>
        /// <param name="group">The project group (may be null).</param>
        /// <param name="name">The project name.</param>
        /// <param name="version">The project version.</param>
        /// <param name="type">The project type (typically application or library).</param>
        public void Begin(string group, string name, string version, string type)
        {
            this.writer.WriteStartElement("metadata");

            this.writer.WriteElementString("timestamp", DateTime.UtcNow.ToString("o"));

            this.writer.WriteStartElement("component");
            this.writer.WriteAttributeString("type", type);

            if (!string.IsNullOrEmpty(group))
                this.writer.WriteElementString("group", group);

            this.writer.WriteElementString("name", name);
            this.writer.WriteElementString("version", version);

            this.writer.WriteEndElement(); // component

            this.writer.WriteEndElement(); // metadata

            this.writer.WriteStartElement("components");
        }
        /// <summary>
        /// Records package information in the SBOM.
        /// </summary>
        /// <param name="group">The package group (may be null).</param>
        /// <param name="name">The package name.</param>
        /// <param name="version">The package version.</param>
        /// <param name="type">The package type (nuget,npm,pypi).</param>
        public void AddPackage(string group, string name, string version, string type)
        {
            this.writer.WriteStartElement("component");
            this.writer.WriteAttributeString("type", "library");

            if (!string.IsNullOrEmpty(group))
                this.writer.WriteElementString("group", group);

            this.writer.WriteElementString("name", name);
            this.writer.WriteElementString("version", version);

            var fullName = string.IsNullOrEmpty(group) ? name : $"{group}/{name}";

            this.writer.WriteElementString("purl", $"pkg:{type}/{Uri.EscapeUriString(fullName)}@{version}");

            this.writer.WriteEndElement(); // component
        }
        /// <summary>
        /// Closes the SBOM file.
        /// </summary>
        public void Dispose()
        {
            this.writer.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
