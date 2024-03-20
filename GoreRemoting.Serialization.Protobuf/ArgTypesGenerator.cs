using System;
using System.Collections.Generic;
using System.Text;

namespace GoreRemoting.Serialization.Protobuf
{
	internal class Generator
	{
		internal static void Generate()
		{
			var max = 20;

			StringBuilder sb = new StringBuilder();
			for (int i = 1; i <= max; i++)
				sb.Append(GenerateType(i));

			sb.AppendLine();
			sb.Append(GenerateTypeGetter(max));

			var res = sb.ToString();
		}

		private static string GenerateTypeGetter(int num)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine("private static Type GetArgsType(Type[] types)");
			sb.AppendLine("{");
			sb.AppendLine("var type = types.Length switch");
			sb.AppendLine("{");

			for (int i = 1; i <= num; i++)
			{
				var pad = "".PadRight(i - 1, ',');
				sb.AppendLine($"{i} => typeof(Args<{pad}>).MakeGenericType(types),");
			}

			sb.AppendLine("_ => throw new NotImplementedException(\"Too many arguments\")");
			sb.AppendLine("};");
			sb.AppendLine("return type;");
			sb.AppendLine("}");

			return sb.ToString();
		}

		internal static string GenerateType(int args)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine("[ProtoContract]");

			string clas = "public class Args<";

			List<string> tees = new List<string>();
			List<string> argss = new List<string>();
			for (int i = 1; i <= args; i++)
			{
				tees.Add("T" + i);
				argss.Add("Arg" + i);
			}

			clas += string.Join(", ", tees);

			clas += "> : IArgs";
			sb.AppendLine(clas);
			sb.AppendLine("{");
			for (int i = 1; i <= args; i++)
			{
				sb.AppendLine($"[ProtoMember({i})]");
				sb.AppendLine($"public T{i}? Arg{i} {{ get; set; }}");
			}

			sb.AppendLine($"public object?[] Get() => new object?[] {{ {string.Join(", ", argss)} }};");

			sb.AppendLine("public void Set(object?[] args)");
			sb.AppendLine("{");
			for (int i = 1; i <= args; i++)
				sb.AppendLine($"Arg{i} = (T{i}?)args[{i - 1}];");
			sb.AppendLine("}");

			sb.AppendLine("}");

			sb.AppendLine();

			return sb.ToString();
		}
	}
}
