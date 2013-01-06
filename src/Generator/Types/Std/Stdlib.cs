namespace Cxxi.Types.Std
{
    [TypeMap("std::string")]
    [TypeMap("std::wstring")]
    public class String : TypeMap
    {
        public override string Signature()
        {
            return "System::String^";
        }

        public override string MarshalToNative(MarshalContext ctx)
        {
            return string.Format("marshalString<E_UTF8>({0})", ctx.Parameter.Name);
        }

        public override string MarshalFromNative(MarshalContext ctx)
        {
            return string.Format("marshalString<E_UTF8>({0})", ctx.ReturnVarName);
        }
    }

    [TypeMap("std::vector")]
    public class Vector : TypeMap
    {
        public override string Signature()
        {
            var type = Type as TemplateSpecializationType;
            var typeName = type.Arguments[0].Type.ToString();
            return string.Format("System::Collections::Generic::List<{0}>^", typeName);
        }

        public override string MarshalToNative(MarshalContext ctx)
        {
            throw new System.NotImplementedException();
        }

        public override string MarshalFromNative(MarshalContext ctx)
        {
            throw new System.NotImplementedException();
        }
    }

    [TypeMap("std::map")]
    public class Map : TypeMap
    {
        public override string Signature()
        {
            var type = Type as TemplateSpecializationType;
            return string.Format("System::Collections::Generic::Dictionary<{0}, {1}>^",
                type.Arguments[0].Type,
                type.Arguments[1].Type);
        }

        public override string MarshalToNative(MarshalContext ctx)
        {
            throw new System.NotImplementedException();
        }

        public override string MarshalFromNative(MarshalContext ctx)
        {
            throw new System.NotImplementedException();
        }
    }

    [TypeMap("std::shared_ptr")]
    public class SharedPtr : TypeMap
    {
        public override string Signature()
        {
            throw new System.NotImplementedException();
        }

        public override string MarshalToNative(MarshalContext ctx)
        {
            throw new System.NotImplementedException();
        }

        public override string MarshalFromNative(MarshalContext ctx)
        {
            throw new System.NotImplementedException();
        }
    }
}
