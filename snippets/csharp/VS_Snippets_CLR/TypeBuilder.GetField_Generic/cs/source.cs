﻿//<Snippet1>
using System;
using System.Reflection;
using System.Reflection.Emit;

// Compare the MSIL in this class to the MSIL
// generated by the Reflection.Emit code in class
// Example.
public class Sample<T>
{
  public T Field;
  public static void GM<U>(U val)
  {
    Sample<U> s = new Sample<U>();
    s.Field = val;
    Console.WriteLine(s.Field);
  }
}

public class Example
{
    public static void Main()
    {
        AppDomain myDomain = AppDomain.CurrentDomain;
        AssemblyName myAsmName = 
            new AssemblyName("TypeBuilderGetFieldExample");
        AssemblyBuilder myAssembly = myDomain.DefineDynamicAssembly(
            myAsmName, AssemblyBuilderAccess.Save);
        ModuleBuilder myModule = myAssembly.DefineDynamicModule(
            myAsmName.Name, 
            myAsmName.Name + ".exe");

        // Define the sample type.
        TypeBuilder myType = myModule.DefineType("Sample", 
            TypeAttributes.Class | TypeAttributes.Public);

        // Add a type parameter, making the type generic.
        string[] typeParamNames = {"T"};  
        GenericTypeParameterBuilder[] typeParams = 
            myType.DefineGenericParameters(typeParamNames);

        // Define a default constructor. Normally it would 
        // not be necessary to define the default constructor,
        // but in this case it is needed for the call to
        // TypeBuilder.GetConstructor, which gets the default
        // constructor for the generic type constructed from 
        // Sample<T>, in the generic method GM<U>.
        ConstructorBuilder ctor = myType.DefineDefaultConstructor(
            MethodAttributes.PrivateScope | MethodAttributes.Public |
            MethodAttributes.HideBySig | MethodAttributes.SpecialName |
            MethodAttributes.RTSpecialName);

        // Add a field of type T, with the name Field.
        FieldBuilder myField = myType.DefineField("Field", 
            typeParams[0],
            FieldAttributes.Public);

        // Add a method and make it generic, with a type 
        // parameter named U. Note how similar this is to 
        // the way Sample is turned into a generic type. The
        // method has no signature, because the type of its
        // only parameter is U, which is not yet defined.
        MethodBuilder genMethod = myType.DefineMethod("GM", 
            MethodAttributes.Public | MethodAttributes.Static);
        string[] methodParamNames = {"U"};
        GenericTypeParameterBuilder[] methodParams = 
            genMethod.DefineGenericParameters(methodParamNames);
        
        // Now add a signature for genMethod, specifying U
        // as the type of the parameter. There is no return value
        // and no custom modifiers.
        genMethod.SetSignature(null, null, null, 
            new Type[] { methodParams[0] }, null, null);

        // Emit a method body for the generic method.
        ILGenerator ilg = genMethod.GetILGenerator();
        // Construct the type Sample<U> using MakeGenericType.
        Type SampleOfU = myType.MakeGenericType( methodParams[0] );
        // Create a local variable to store the instance of
        // Sample<U>.
        ilg.DeclareLocal(SampleOfU);
        // Call the default constructor. Note that it is 
        // necessary to have the default constructor for the
        // constructed generic type Sample<U>; use the 
        // TypeBuilder.GetConstructor method to obtain this 
        // constructor.
        ConstructorInfo ctorOfU = TypeBuilder.GetConstructor(
            SampleOfU, ctor);
        ilg.Emit(OpCodes.Newobj, ctorOfU);
        // Store the instance in the local variable; load it
        // again, and load the parameter of genMethod.
        ilg.Emit(OpCodes.Stloc_0); 
        ilg.Emit(OpCodes.Ldloc_0); 
        ilg.Emit(OpCodes.Ldarg_0);
        // In order to store the value in the field of the
        // instance of Sample<U>, it is necessary to have 
        // a FieldInfo representing the field of the 
        // constructed type. Use TypeBuilder.GetField to 
        // obtain this FieldInfo.
        FieldInfo FieldOfU = TypeBuilder.GetField(
            SampleOfU, myField);
        // Store the value in the field. 
        ilg.Emit(OpCodes.Stfld, FieldOfU);
        // Load the instance, load the field value, box it
        // (specifying the type of the type parameter, U), and
        // print it.
        ilg.Emit(OpCodes.Ldloc_0);
        ilg.Emit(OpCodes.Ldfld, FieldOfU);
        ilg.Emit(OpCodes.Box, methodParams[0]);
        MethodInfo writeLineObj = 
            typeof(Console).GetMethod("WriteLine", 
                new Type[] { typeof(object) });
        ilg.EmitCall(OpCodes.Call, writeLineObj, null);
        ilg.Emit(OpCodes.Ret);

        // Emit an entry point method; this must be in a
        // non-generic type.
        TypeBuilder dummy = myModule.DefineType("Dummy", 
            TypeAttributes.Class | TypeAttributes.NotPublic);
        MethodBuilder entryPoint = dummy.DefineMethod("Main", 
            MethodAttributes.Public | MethodAttributes.Static,
            null, null);
        ilg = entryPoint.GetILGenerator();
        // In order to call the static generic method GM, it is
        // necessary to create a constructed type from the 
        // generic type definition for Sample. This can be any
        // constructed type; in this case Sample<int> is used.
        Type SampleOfInt = 
            myType.MakeGenericType( typeof(int) );
        // Next get a MethodInfo representing the static generic
        // method GM on type Sample<int>.
        MethodInfo SampleOfIntGM = TypeBuilder.GetMethod(SampleOfInt, 
            genMethod);
        // Next get a MethodInfo for GM<string>, which is the 
        // instantiation of GM that Main calls.
        MethodInfo GMOfString = 
            SampleOfIntGM.MakeGenericMethod( typeof(string) );
        // Finally, emit the call. Push a string onto
        // the stack, as the argument for the generic method.
        ilg.Emit(OpCodes.Ldstr, "Hello, world!");
        ilg.EmitCall(OpCodes.Call, GMOfString, null);
        ilg.Emit(OpCodes.Ret);

        myType.CreateType();
        dummy.CreateType();
        myAssembly.SetEntryPoint(entryPoint);
        myAssembly.Save(myAsmName.Name + ".exe");

        Console.WriteLine(myAsmName.Name + ".exe has been saved.");
    }
}
//</Snippet1>