using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoreRemoting.Tests.ExternalTypes;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.ComponentModel;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;


[Serializable]
public sealed partial class SqlExceptionTest : System.Data.Common.DbException
{
	private const string OriginalClientConnectionIdKey = "OriginalClientConnectionId";
	private const string RoutingDestinationKey = "RoutingDestination";
	private const int SqlExceptionHResult = unchecked((int)0x80131904);

	private readonly SqlErrorCollection _errors;
#if NETFRAMEWORK
	[System.Runtime.Serialization.OptionalFieldAttribute(VersionAdded = 4)]
#endif
	private Guid _clientConnectionId = Guid.Empty;

//#if NETFRAMEWORK
//	[System.Runtime.Serialization.IgnoreDataMember]
//#endif
	//private SqlBatchCommand _batchCommand;

#if NETFRAMEWORK
	[System.Runtime.Serialization.IgnoreDataMember]
#endif
	// Do not serialize this field! It is used to indicate that no reconnection attempts are required
	internal bool _doNotReconnect = false;

	private SqlExceptionTest(string message, SqlErrorCollection errorCollection, Exception innerException, Guid conId) : base(message, innerException)
	{
		HResult = SqlExceptionHResult;
		_errors = errorCollection;
		_clientConnectionId = conId;
	}
#if NET
        [System.Obsolete]
#endif
	private SqlExceptionTest(SerializationInfo si, StreamingContext sc) : base(si, sc)
	{
#if NETFRAMEWORK
		_errors = (SqlErrorCollection)si.GetValue("Errors", typeof(SqlErrorCollection));
#endif
		HResult = SqlExceptionHResult;
		foreach (SerializationEntry siEntry in si)
		{
			if (nameof(ClientConnectionId) == siEntry.Name)
			{
				_clientConnectionId = (Guid)si.GetValue(nameof(ClientConnectionId), typeof(Guid));
				break;
			}
		}
	}

	/// <inheritdoc cref="System.Exception.GetObjectData" />
#if NET
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
#endif
	public override void GetObjectData(SerializationInfo si, StreamingContext context)
	{
		base.GetObjectData(si, context);
		si.AddValue("Errors", null); // Not specifying type to enable serialization of null value of non-serializable type
		si.AddValue("ClientConnectionId", _clientConnectionId, typeof(object));

		// Writing sqlerrors to base exception data table
		for (int i = 0; i < Errors.Count; i++)
		{
			string key = "SqlError " + (i + 1);
			if (Data.Contains(key))
			{
				Data.Remove(key);
			}
			Data.Add(key, Errors[i].ToString());
		}
	}

	// runtime will call even if private...
#if NETFRAMEWORK
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
#endif
	public SqlErrorCollection Errors => _errors ?? new SqlErrorCollection();
	public Guid ClientConnectionId => _clientConnectionId;
	public byte Class => Errors.Count > 0 ? Errors[0].Class : default;
	public int LineNumber => Errors.Count > 0 ? Errors[0].LineNumber : default;
	public int Number => Errors.Count > 0 ? Errors[0].Number : default;
	public string Procedure => Errors.Count > 0 ? Errors[0].Procedure : default;
	public string Server => Errors.Count > 0 ? Errors[0].Server : default;
	public byte State => Errors.Count > 0 ? Errors[0].State : default;
	override public string Source => "TdsEnums.SQL_PROVIDER_NAME";


	public override string ToString()
	{
		StringBuilder sb = new(base.ToString());
		sb.AppendLine();
		sb.AppendFormat(SQLMessage.ExClientConnectionId(), _clientConnectionId);

		// Append the error number, state and class if the server provided it
		if (Errors.Count > 0 && Number != 0)
		{
			sb.AppendLine();
			sb.AppendFormat(SQLMessage.ExErrorNumberStateClass(), Number, State, Class);
		}

		// If routed, include the original client connection id
		if (Data.Contains(OriginalClientConnectionIdKey))
		{
			sb.AppendLine();
			sb.AppendFormat(SQLMessage.ExOriginalClientConnectionId(), Data[OriginalClientConnectionIdKey]);
		}

		// If routed, provide the routing destination
		if (Data.Contains(RoutingDestinationKey))
		{
			sb.AppendLine();
			sb.AppendFormat(SQLMessage.ExRoutingDestination(), Data[RoutingDestinationKey]);
		}

		return sb.ToString();
	}


	// NOTE: do not combine the overloads below using an optional parameter
	//  they must remain distinct because external projects use private reflection
	//  to find and invoke the functions, changing the signatures will break many
	//  things elsewhere
	// @TODO: Don't be ridiculous, no external project should take a dependency on internal/private methods. Otherwise they would've been made public APIs.

	public static SqlExceptionTest CreateException(
		SqlError error,
		string serverVersion,
		SqlInternalConnectionTds internalConnection,
		Exception innerException = null)
	{
		SqlErrorCollection errorCollection = new() { error };
		return CreateException(errorCollection, serverVersion, internalConnection, innerException);
	}

	internal static SqlExceptionTest CreateException(SqlErrorCollection errorCollection, string serverVersion) =>
		CreateException(errorCollection, serverVersion, Guid.Empty, innerException: null);

	internal static SqlExceptionTest CreateException(
		SqlErrorCollection errorCollection,
		string serverVersion,
		SqlInternalConnectionTds internalConnection,
		Exception innerException = null
		//SqlBatchCommand batchCommand = null
		)
	{
		Guid connectionId = (internalConnection == null) ? Guid.Empty : internalConnection._clientConnectionId;
		SqlExceptionTest exception = CreateException(errorCollection, serverVersion, connectionId, innerException);

		if (internalConnection != null)
		{
			if ((internalConnection.OriginalClientConnectionId != Guid.Empty) && (internalConnection.OriginalClientConnectionId != internalConnection.ClientConnectionId))
			{
				exception.Data.Add(OriginalClientConnectionIdKey, internalConnection.OriginalClientConnectionId);
			}

			if (!string.IsNullOrEmpty(internalConnection.RoutingDestination))
			{
				exception.Data.Add(RoutingDestinationKey, internalConnection.RoutingDestination);
			}
		}

		return exception;
	}

	internal static SqlExceptionTest CreateException(
		SqlErrorCollection errorCollection,
		string serverVersion,
		Guid conId,
		Exception innerException = null
		//SqlBatchCommand batchCommand = null
		)
	{
		Debug.Assert(errorCollection != null && errorCollection.Count > 0, "no errorCollection?");

		StringBuilder message = new();
		for (int i = 0; i < errorCollection.Count; i++)
		{
			if (i > 0)
			{
				message.Append(Environment.NewLine);
			}
			message.Append(errorCollection[i].Message);
		}

		if (innerException == null && errorCollection[0].Win32ErrorCode != 0 && errorCollection[0].Win32ErrorCode != -1)
		{
			innerException = new Win32Exception(errorCollection[0].Win32ErrorCode);
		}

		SqlExceptionTest exception = new(message.ToString(), errorCollection, innerException, conId);
//		exception.BatchCommand = batchCommand;
		exception.Data.Add("HelpLink.ProdName", "Microsoft SQL Server");
		if (!string.IsNullOrEmpty(serverVersion))
		{
			exception.Data.Add("HelpLink.ProdVer", serverVersion);
		}
		exception.Data.Add("HelpLink.EvtSrc", "MSSQLServer");
		exception.Data.Add("HelpLink.EvtID", errorCollection[0].Number.ToString(CultureInfo.InvariantCulture));
		exception.Data.Add("HelpLink.BaseHelpUrl", "https://go.microsoft.com/fwlink");
		exception.Data.Add("HelpLink.LinkId", "20476");

		return exception;
	}

	internal SqlExceptionTest InternalClone()
	{
		SqlExceptionTest exception = new(Message, _errors, InnerException, _clientConnectionId);
		if (Data != null)
		{
			foreach (DictionaryEntry entry in Data)
			{
				exception.Data.Add(entry.Key, entry.Value);
			}
		}
		//exception._batchCommand = _batchCommand;
		exception._doNotReconnect = _doNotReconnect;
		return exception;
	}
}


public  class SQLMessage
{
	internal static string ExClientConnectionId()
	{
		return "ExClientConnectionId";
	}

	internal static string ExErrorNumberStateClass()
	{
		return "ExErrorNumberStateClass";
	}

	internal static string ExOriginalClientConnectionId()
	{
		return "ExOriginalClientConnectionId";
	}

	internal static string ExRoutingDestination()
	{
		return "ExRoutingDestination";
	}
}

public class SqlInternalConnectionTds
{
	internal Guid _clientConnectionId;

	public Guid OriginalClientConnectionId { get; internal set; }
	public string RoutingDestination { get; internal set; }
	public Guid ClientConnectionId { get; internal set; }
}