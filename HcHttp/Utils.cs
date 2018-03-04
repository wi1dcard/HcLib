using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSScriptControl;

namespace HcHttp
{
    public static class Utils
    {
		public static ScriptControl Script(string Script)
		{
			ScriptControl sc = new ScriptControl();
			sc.AllowUI = false;
			sc.Language = "JavaScript";
			sc.AddCode(Script);
			return sc;
		}

		public static string Between(this string Source, string Begin, string End)
        {
            int iBegin = Source.IndexOf(Begin);
            if (iBegin < 0)
            {
                return null;
            }
            int iEnd = Source.IndexOf(End, iBegin + Begin.Length);
            if (iEnd < 0)
            {
                return null;
            }
            return Source.Substring(iBegin + Begin.Length, iEnd - iBegin - Begin.Length);
        }

        public static object SelfCopy(this object obj)
        {
            Object targetDeepCopyObj;
            Type targetType = obj.GetType();
            //值类型  
            if (targetType.IsValueType == true)
            {
                targetDeepCopyObj = obj;
            }
            //引用类型   
            else
            {
                targetDeepCopyObj = System.Activator.CreateInstance(targetType);   //创建引用对象   
                System.Reflection.MemberInfo[] memberCollection = obj.GetType().GetMembers();

                foreach (System.Reflection.MemberInfo member in memberCollection)
                {
                    if (member.MemberType == System.Reflection.MemberTypes.Field)
                    {
                        System.Reflection.FieldInfo field = (System.Reflection.FieldInfo)member;
                        Object fieldValue = field.GetValue(obj);
                        if (fieldValue is ICloneable)
                        {
                            field.SetValue(targetDeepCopyObj, (fieldValue as ICloneable).Clone());
                        }
                        else
                        {
                            field.SetValue(targetDeepCopyObj, SelfCopy(fieldValue));
                        }

                    }
                    else if (member.MemberType == System.Reflection.MemberTypes.Property)
                    {
                        System.Reflection.PropertyInfo myProperty = (System.Reflection.PropertyInfo)member;
                        System.Reflection.MethodInfo info = myProperty.GetSetMethod(false);
                        if (info != null)
                        {
                            object propertyValue = myProperty.GetValue(obj, null);
                            if (propertyValue is ICloneable)
                            {
                                myProperty.SetValue(targetDeepCopyObj, (propertyValue as ICloneable).Clone(), null);
                            }
                            else
                            {
                                myProperty.SetValue(targetDeepCopyObj, SelfCopy(propertyValue), null);
                            }
                        }

                    }
                }
            }
            return targetDeepCopyObj;
        }
    }
}
