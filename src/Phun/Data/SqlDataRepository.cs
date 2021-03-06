﻿namespace Phun.Data
{
    using System;
    using System.IO;
    using System.Linq;

    using Dapper;

    using Phun.Extensions;

    /// <summary>
    /// Use by SQL Server Repository to store and retrieve data.
    /// </summary>
    public class SqlDataRepository : ISqlDataRepository
    {
        /// <summary>
        /// Populates the data.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="content">The content.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="cachePath">The cache path.</param>
        /// <returns>
        /// Content model with populated data stream.
        /// </returns>
        public virtual ContentModel PopulateData(DapperContext context, ContentModel content, string tableName, string cachePath)
        {
            this.CacheData(context, content, tableName, cachePath);

            if (!string.IsNullOrEmpty(cachePath))
            {
                var localPath = this.ResolvePath(content, cachePath);

                // return a stream
                content.DataStream = new System.IO.FileStream(
                    localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }

            return content;
        }

        /// <summary>
        /// Caches the data.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="content">The content.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="cachePath">The cache path.</param>
        public virtual void CacheData(DapperContext context, ContentModel content, string tableName, string cachePath)
        {
            if (!string.IsNullOrEmpty(content.DataIdString))
            {
                var data =
                    context.Query<ContentModel>(
                        string.Format("SELECT Data, DataLength FROM {0} WHERE IdString = @DataIdString", tableName), content).FirstOrDefault();

                if (data != null)
                {
                    content.Data = data.Data;
                    content.DataLength = data.DataLength;
                }
            }

            if (string.IsNullOrEmpty(cachePath))
            {
                return;
            }

            // determine if local content exists or is out of date
            var localPath = this.ResolvePath(content, cachePath);
            var canCache = !File.Exists(localPath);
            if (!canCache)
            {
                var lastWriteTime = File.GetLastWriteTime(localPath);
                canCache = lastWriteTime < (content.ModifyDate ?? content.CreateDate);
            }

            if (canCache)
            {
                var localDir = System.IO.Path.GetDirectoryName(localPath);
                if (!System.IO.Directory.Exists(localDir))
                {
                    System.IO.Directory.CreateDirectory(localDir);
                }

                System.IO.File.WriteAllBytes(localPath, content.Data ?? new byte[0]);
            }
        }

        /// <summary>
        /// Saves the data.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="content">The content.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="cachePath">The cache path.</param>
        /// <param name="keepHistory">if set to <c>true</c> [keep history].</param>
        public virtual void SaveData(DapperContext context, ContentModel content, string tableName, string cachePath)
        {
            var newDataId = Guid.NewGuid();
            var newContent = new ContentModel()
                                 {
                                     DataId = newDataId,
                                     Host = content.Host,
                                     Path = content.Path,
                                     CreateBy =
                                         string.IsNullOrEmpty(content.ModifyBy)
                                             ? content.CreateBy
                                             : content.ModifyBy,
                                     Data =
                                         content.Data ?? content.DataStream.ReadAll(),
                                 };

            newContent.DataLength = newContent.Data.Length;
            content.DataLength = newContent.DataLength;
            newContent.ModifyDate = DateTime.UtcNow;
            if (!newContent.CreateDate.HasValue || newContent.CreateDate.Value == DateTime.MinValue)
            {
                newContent.CreateDate = DateTime.UtcNow;
            }

            context.Execute(string.Format("DELETE FROM {0} WHERE Host = @Host AND Path = @Path", tableName), newContent);
            context.Execute(
                    string.Format("INSERT INTO {0} (IdString, Host, Path, Data, DataLength, CreateDate, CreateBy) VALUES (@DataIdString, @Host, @Path, @Data, @DataLength, @CreateDate, @CreateBy)", tableName), newContent);
            content.Data = newContent.Data;
            content.DataLength = newContent.DataLength;
            content.DataId = newContent.DataId;
            this.CacheData(context, content, tableName, cachePath);
        }

        /// <summary>
        /// Retrieves the history.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="content">The content.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>
        /// The content history.
        /// </returns>
        public virtual IQueryable<ContentModel> RetrieveHistory(DapperContext context, ContentModel content, string tableName)
        {
            var result = context.Query<ContentModel>(string.Format("SELECT IdString AS DataIdString, Host, Path, DataLength, CreateDate, CreateBy FROM {0} WHERE Host = @Host and Path = @Path", tableName), content);
            return result.AsQueryable();
        }

        /// <summary>
        /// Populates the history data.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="content">The content.</param>
        /// <param name="tableName">Name of the table.</param>
        public void PopulateHistoryData(DapperContext context, ContentModel content, string tableName)
        {
            if (string.IsNullOrEmpty(content.DataIdString))
            {
                throw new ArgumentException("PopulateHistoryData content.DataIdString is required.", "content");
            }

            var data =
                context.Query<ContentModel>(
                    string.Format("SELECT Data, DataLength FROM {0} WHERE IdString = @DataIdString AND Host = @Host AND Path = @Path", tableName),
                    content).FirstOrDefault();

            if (data == null)
            {
                return;
            }

            content.Data = data.Data;
            content.DataLength = data.DataLength;
        }

        /// <summary>
        /// Resolves the path.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="basePath">The base path.</param>
        /// <returns>
        /// The full path to the content or file.
        /// </returns>
        /// <exception cref="System.ArgumentException">Content is required.;content
        /// or
        /// Content path is required or not valid:  + content.Path;content.Path</exception>
        private string ResolvePath(ContentModel content, string basePath)
        {
            bool isFolder = content.Path.EndsWith("/", StringComparison.OrdinalIgnoreCase);

            if (content == null)
            {
                throw new ArgumentException("Content is required.", "content");
            }


            // add: 'basePath\host\contentPath'
            var result = string.Concat(basePath, "\\", content.Host, "\\", content.Path.Trim('/').Replace("/", "\\"));

            // make sure that there is no illegal path
            result = result.Replace("..", string.Empty).Replace("\\\\", "\\").TrimEnd('\\');

            // result full path must not be more than 3 characters
            if (result.Length <= 3)
            {
                throw new ArgumentException("Illegal path detected: " + content.Path, "path");
            }

            if (isFolder)
            {
                result = result + "\\";
            }


            var isValidChildOfBasePath = System.IO.Path.GetFullPath(result)
                        .StartsWith(System.IO.Path.GetFullPath(basePath), StringComparison.OrdinalIgnoreCase);

            if (!isValidChildOfBasePath)
            {
                throw new ArgumentException("Illegal path access: " + content.Path, "path");
            }

            return result;
        }
    }
}
