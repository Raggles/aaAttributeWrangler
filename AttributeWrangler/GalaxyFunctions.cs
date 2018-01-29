using ArchestrA.GRAccess;

namespace AttributeWrangler
{
    public static class GalaxyFunctions
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void UpdateMxReference(bool whatif, ArchestrAObject obj, IAttribute attribute, Operation op, string newValue, string findString = "")
        {
            if (attribute.DataType != MxDataType.MxReferenceType)
            {
                _log.Error(string.Format("Attribute data type {0} is not supported by function {1}", attribute.DataType.ToString(), nameof(UpdateMxReference)));
            }
            IMxReference mxref = attribute.value.GetMxReference();
            findString = findString.Replace("~%obj", obj.Name);
            newValue = newValue.Replace("~%obj", obj.Name);

            switch (op)
            {
                case Operation.FindReplace:
                    if (mxref.FullReferenceString.Contains(findString))
                        mxref.FullReferenceString = mxref.FullReferenceString.Replace(findString, newValue);
                    else
                        return;
                    break;
                case Operation.FindUpdate:
                    if (mxref.FullReferenceString == findString)
                    {
                        mxref.FullReferenceString = newValue;
                    }
                    break;
                case Operation.Update:
                    mxref.FullReferenceString = newValue;
                    break;
                default:
                    _log.Error(string.Format("{0} opertation is not supported by function {1}", op.ToString(), nameof(UpdateMxReference)));
                    return;
            }

            _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", attribute.Name, obj.Name, attribute.value.GetString(), mxref.FullReferenceString));

            if (!whatif)
            {
                MxValue mxval = new MxValueClass();
                mxval.PutMxReference(mxref);
                attribute.SetValue(mxval);

                ICommandResult cmd = attribute.CommandResult;
                if (!cmd.Successful)
                {
                    _log.Warn(string.Format("{0} Failed:{1}:{2}", nameof(UpdateMxReference), cmd.Text, cmd.CustomMessage));
                }
            }
        }

        public static void UpdateMxString(bool whatif, ArchestrAObject obj, IAttribute attribute, Operation op, string newValue, string findString = "")
        {
            if (attribute.DataType != MxDataType.MxString)
            {
                _log.Error(string.Format("Attribute data type {0} is not supported by function {1}", attribute.DataType.ToString(), nameof(UpdateMxString)));
            }
            string newString = attribute.value.GetString();
            findString = findString.Replace("~%obj", obj.Name);
            newValue = newValue.Replace("~%obj", obj.Name);

            switch (op)
            {
                case Operation.FindReplace:
                    if (newString.Contains(findString))
                    {
                        newString = newString.Replace(findString, newValue);
                    }
                    break;
                case Operation.FindUpdate:
                    if (newString == findString)
                        newString = newValue;
                    else
                        return;
                    break;
                case Operation.Update:
                    newString = newValue;
                    break;
                default:
                    _log.Error(string.Format("{0} opertation is not supported by function {1}", op.ToString(), nameof(UpdateMxString)));
                    return;
            }

            _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", attribute.Name, obj.Name, attribute.value.GetString(), newString));

            if (!whatif)
            {
                MxValue mxval = new MxValueClass();
                mxval.PutString(newString);
                attribute.SetValue(mxval);
                ICommandResult cmd = attribute.CommandResult;
                if (!cmd.Successful)
                {
                    _log.Warn(string.Format("{0} Failed:{1}:{2}", nameof(UpdateMxString), cmd.Text, cmd.CustomMessage));
                }
            }
        }

        public static void UpdateMxInteger(bool whatif, ArchestrAObject obj, IAttribute attribute, Operation op, int newValue, int findInt = 0)
        {
            if (attribute.DataType != MxDataType.MxInteger)
            {
                _log.Error(string.Format("Attribute data type {0} is not supported by function {1}", attribute.DataType.ToString(), nameof(UpdateMxInteger)));
            }
            int newInt = attribute.value.GetInteger();

            switch (op)
            {
                case Operation.FindUpdate:
                    if (newInt == findInt)
                        newInt = newValue;
                    else
                        return;
                    break;
                case Operation.Update:
                    newInt = newValue;
                    break;
                default:
                    _log.Error(string.Format("{0} opertation is not supported by function {1}", op.ToString(), nameof(UpdateMxInteger)));
                    return;
            }

            _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", attribute.Name, obj.Name, attribute.value.GetString(), newInt));

            if (!whatif)
            {
                MxValue mxval = new MxValueClass();
                mxval.PutInteger(newInt);
                attribute.SetValue(mxval);

                ICommandResult cmd = attribute.CommandResult;
                if (!cmd.Successful)
                {
                    _log.Warn(string.Format("{0} Failed:{1}:{2}", nameof(UpdateMxInteger), cmd.Text, cmd.CustomMessage));
                }
            }
        }

        public static void UpdateMxDouble(bool whatif, ArchestrAObject obj, IAttribute attribute, Operation op, double newValue, double findDbl = 0)
        {
            if (attribute.DataType != MxDataType.MxDouble)
            {
                _log.Error(string.Format("Attribute data type {0} is not supported by function {1}", attribute.DataType.ToString(), nameof(UpdateMxDouble)));
            }
            double newDouble = attribute.value.GetDouble();
            
            switch (op)
            {
                case Operation.FindUpdate:
                    if (newDouble == findDbl)
                        newDouble = newValue;
                    else
                        return;
                    break;
                case Operation.Update:
                    newDouble = newValue;
                    break;
                default:
                    _log.Error(string.Format("{0} opertation is not supported by function {1}", op.ToString(), nameof(UpdateMxDouble)));
                    return;
            }

            _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", attribute.Name, obj.Name, attribute.value.GetString(), newDouble));

            if (!whatif)
            {
                MxValue mxval = new MxValueClass();
                mxval.PutDouble(newDouble);
                attribute.SetValue(mxval);

                ICommandResult cmd = attribute.CommandResult;
                if (!cmd.Successful)
                {
                    _log.Warn(string.Format("{0} Failed:{1}:{2}", nameof(UpdateMxDouble), cmd.Text, cmd.CustomMessage));
                }
            }
        }

        public static void UpdateMxFloat(bool whatif, ArchestrAObject obj, IAttribute attribute, Operation op, float newValue, float findFlt = 0)
        {
            if (attribute.DataType != MxDataType.MxFloat)
            {
                _log.Error(string.Format("Attribute data type {0} is not supported by function {1}", attribute.DataType.ToString(), nameof(UpdateMxFloat)));
            }
            float newFloat = attribute.value.GetFloat();

            switch (op)
            {
                case Operation.FindUpdate:
                    if (newFloat == findFlt)
                        newFloat = newValue;
                    else
                        return;
                    break;
                case Operation.Update:
                    newFloat = newValue;
                    break;
                default:
                    _log.Error(string.Format("{0} opertation is not supported by function {1}", op.ToString(), nameof(UpdateMxFloat)));
                    return;
            }

            _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", attribute.Name, obj.Name, attribute.value.GetString(), newFloat));

            if (!whatif)
            {
                MxValue mxval = new MxValueClass();
                mxval.PutFloat(newFloat);
                attribute.SetValue(mxval);

                ICommandResult cmd = attribute.CommandResult;
                if (!cmd.Successful)
                {
                    _log.Warn(string.Format("{0} Failed:{1}:{2}", nameof(UpdateMxFloat), cmd.Text, cmd.CustomMessage));
                }
            }
        }

        public static void UpdateMxBool(bool whatif, ArchestrAObject obj, IAttribute attribute, Operation op, bool newValue, bool findBool = false)
        {
            if (attribute.DataType != MxDataType.MxBoolean)
            {
                _log.Error(string.Format("Attribute data type {0} is not supported by function {1}", attribute.DataType.ToString(), nameof(UpdateMxBool)));
            }
            bool newBool = attribute.value.GetBoolean();

            switch (op)
            {
                case Operation.FindUpdate:
                    if (newBool == findBool)
                        newBool = newValue;
                    else
                        return;
                    break;
                case Operation.Update:
                    newBool = newValue;
                    break;
                default:
                    _log.Error(string.Format("{0} opertation is not supported by function {1}", op.ToString(), nameof(UpdateMxBool)));
                    return;
            }

            _log.Info(string.Format("Updating attribute [{0}] on object [{1}] from [{2}] to [{3}]", attribute.Name, obj.Name, attribute.value.GetString(), newBool));

            if (!whatif)
            {
                MxValue mxval = new MxValueClass();
                mxval.PutBoolean(newBool);
                attribute.SetValue(mxval);

                ICommandResult cmd = attribute.CommandResult;
                if (!cmd.Successful)
                {
                    _log.Warn(string.Format("{0} Failed:{1}:{2}", nameof(UpdateMxBool), cmd.Text, cmd.CustomMessage));
                }
            }
        }

    }
}
