﻿/*
 *    The contents of this file are subject to the Initial
 *    Developer's Public License Version 1.0 (the "License");
 *    you may not use this file except in compliance with the
 *    License. You may obtain a copy of the License at
 *    https://github.com/FirebirdSQL/NETProvider/blob/master/license.txt.
 *
 *    Software distributed under the License is distributed on
 *    an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *    express or implied. See the License for the specific
 *    language governing rights and limitations under the License.
 *
 *    All Rights Reserved.
 */

//$Authors = Jiri Cincura (jiri@cincura.net)

using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NUnit.Framework;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Tests.Query
{
	public class ElementaryTests : EntityFrameworkCoreTestsBase
	{
		[Test]
		public void SimpleSelect()
		{
			using (var db = GetDbContext<SelectContext>())
			{
				var data = db.Set<MonAttachment>().ToList();
				Assert.IsNotEmpty(data);
			}
		}

		[Test]
		public void SelectWithWhere()
		{
			using (var db = GetDbContext<SelectContext>())
			{
				var query = db.Set<MonAttachment>()
					.Where(x => x.AttachmentName.Trim() != string.Empty);
				Assert.DoesNotThrow(() => query.Load());
				var sql = db.LastCommandText;
				StringAssert.Contains("TRIM(", sql);
			}
		}

		[Test]
		public void SelectWithWhereExtract()
		{
			using (var db = GetDbContext<SelectContext>())
			{
				var query = db.Set<MonAttachment>()
					.Where(x => x.Timestamp.Second > -1 && x.Timestamp.DayOfYear == 1);
				Assert.DoesNotThrow(() => query.Load());
				var sql = db.LastCommandText;
			}
		}

		[Test]
		public void SelectWithWhereSubstring()
		{
			using (var db = GetDbContext<SelectContext>())
			{
				var query = db.Set<MonAttachment>()
					.Where(x => x.AttachmentName.Substring(1) == string.Empty && x.AttachmentName.Substring(1, 1) == string.Empty || x.AttachmentName.Substring(x.AttachmentId) != string.Empty || x.AttachmentName.Substring(x.AttachmentId, x.AttachmentId) != string.Empty);
				Assert.DoesNotThrow(() => query.Load());
				var sql = db.LastCommandText;
			}
		}

		[Test]
		public void SelectWithWhereDateMember()
		{
			using (var db = GetDbContext<SelectContext>())
			{
				var query = db.Set<MonAttachment>()
					.Where(x => x.Timestamp.Date == DateTime.Now.Date);
				Assert.DoesNotThrow(() => query.Load());
				var sql = db.LastCommandText;
			}
		}

		[Test]
		public void SelectTake()
		{
			using (var db = GetDbContext<SelectContext>())
			{
				var query = db.Set<MonAttachment>()
					.Take(3);
				Assert.DoesNotThrow(() => query.Load());
				var sql = db.LastCommandText;
				StringAssert.IsMatch(@"ROWS \(.+\)", sql);
				StringAssert.DoesNotMatch(@" TO \(", sql);
			}
		}

		[Test]
		public void SelectSkipTake()
		{
			using (var db = GetDbContext<SelectContext>())
			{
				var query = db.Set<MonAttachment>()
					.Skip(1)
					.Take(3);
				Assert.DoesNotThrow(() => query.Load());
				var sql = db.LastCommandText;
				StringAssert.IsMatch(@"ROWS \((.+) \+ 1\) TO \(\1 \+ .+\)", sql);
			}
		}

		[Test]
		public void SelectSkip()
		{
			using (var db = GetDbContext<SelectContext>())
			{
				var query = db.Set<MonAttachment>()
					.Skip(1);
				Assert.DoesNotThrow(() => query.Load());
				var sql = db.LastCommandText;
				StringAssert.IsMatch(@"ROWS \(.+ \+ 1\) TO \(9223372036854775807\)", sql);
			}
		}

		[Test]
		public void SelectTopLevelAny()
		{
			if (!EnsureVersion(new Version(3, 0, 0, 0)))
				return;

			using (var db = GetDbContext<SelectContext>())
			{
				Assert.DoesNotThrow(() => db.Set<MonAttachment>().Any(x => x.AttachmentId != 0));
			}
		}
	}

	class SelectContext : FbTestDbContext
	{
		class LastCommandTextCommandInterceptor : DbCommandInterceptor
		{
			public string LastCommandText { get; private set; }

			public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
			{
				LastCommandText = command.CommandText;
				return base.NonQueryExecuted(command, eventData, result);
			}

			public override Task<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
			{
				LastCommandText = command.CommandText;
				return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
			}

			public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
			{
				LastCommandText = command.CommandText;
				return base.ReaderExecuted(command, eventData, result);
			}

			public override Task<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
			{
				LastCommandText = command.CommandText;
				return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
			}

			public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
			{
				LastCommandText = command.CommandText;
				return base.ScalarExecuted(command, eventData, result);
			}

			public override Task<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result, CancellationToken cancellationToken = default)
			{
				LastCommandText = command.CommandText;
				return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
			}
		}

		LastCommandTextCommandInterceptor _lastCommandTextInterceptor;

		public SelectContext(string connectionString)
			: base(connectionString)
		{
			_lastCommandTextInterceptor = new LastCommandTextCommandInterceptor();
		}

		protected override void OnTestModelCreating(ModelBuilder modelBuilder)
		{
			base.OnTestModelCreating(modelBuilder);

			var monAttachmentConf = modelBuilder.Entity<MonAttachment>();
			monAttachmentConf.HasKey(x => x.AttachmentId);
			monAttachmentConf.Property(x => x.AttachmentId).HasColumnName("MON$ATTACHMENT_ID");
			monAttachmentConf.Property(x => x.AttachmentName).HasColumnName("MON$ATTACHMENT_NAME");
			monAttachmentConf.Property(x => x.Timestamp).HasColumnName("MON$TIMESTAMP");
			monAttachmentConf.ToTable("MON$ATTACHMENTS");
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			base.OnConfiguring(optionsBuilder);


			optionsBuilder.AddInterceptors(_lastCommandTextInterceptor);
		}

		public string LastCommandText => _lastCommandTextInterceptor.LastCommandText;
	}

	class MonAttachment
	{
		public int AttachmentId { get; set; }
		public string AttachmentName { get; set; }
		public DateTime Timestamp { get; set; }
	}
}
