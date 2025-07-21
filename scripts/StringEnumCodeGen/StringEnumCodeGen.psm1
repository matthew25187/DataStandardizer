function Get-ValueFieldDeclaration {
    [OutputType([System.CodeDom.CodeMemberField])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )

    $typeExpression = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? 'readonly string?':'readonly string'
    $fieldDeclaration = [System.CodeDom.CodeMemberField]::new($typeExpression, '_value')
    $fieldDeclaration.Attributes = ($fieldDeclaration.Attributes -band -bnot [System.CodeDom.MemberAttributes]::AccessMask) -bor [System.CodeDom.MemberAttributes]::Private

    if (-not $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')) {
        [void]$fieldDeclaration.CustomAttributes.Add([System.CodeDom.CodeAttributeDeclaration]::new('JetBrains.Annotations.CanBeNullAttribute'))
    }

    return $fieldDeclaration
}

function Get-EqualsMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [string]    $TypeNamespace,

        [Parameter(Mandatory)]
        [string]    $TypeName
    )

    $methodName = 'Equals'
    $typeReference = Get-TypeFullName $TypeNamespace $TypeName

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::AccessMask) -bor [System.CodeDom.MemberAttributes]::Public
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::ScopeMask) -bor [System.CodeDom.MemberAttributes]::Final
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([bool])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($typeReference, 'other'))

    $equatableTypeReference = [System.CodeDom.CodeTypeReference]::new('System.IEquatable')
    [void]$equatableTypeReference.TypeArguments.Add($typeReference)
    [void]$method.ImplementationTypes.Add([System.CodeDom.CodeTypeReference]::new($equatableTypeReference))

    # Define method statements.
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeArgumentReferenceExpression]::new('other'), '_value')))
    [void]$method.Statements.Add($returnStatement)

    return $method
}

function Get-GetHashCodeMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]

    $methodName = 'GetHashCode'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::AccessMask) -bor [System.CodeDom.MemberAttributes]::Public
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::ScopeMask) -bor [System.CodeDom.MemberAttributes]::Override
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([int])

    # Define method statements.
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new([System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'), [System.CodeDom.CodeBinaryOperatorType]::ValueEquality, [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodePrimitiveExpression]::new(0))))
    $mainReturnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'), 'GetHashCode', @()))
    $method.Statements.AddRange(@($nullCheckStatement, $mainReturnStatement))

    return $method
}

function Get-MemberFieldPredicateMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [string]    $TypeNamespace,

        [Parameter(Mandatory)]
        [string]    $TypeName,

        [Parameter(Mandatory)]
        [string]    $GenerateLanguage
    )
    
    $provider = [System.CodeDom.Compiler.CodeDomProvider]::CreateProvider($GenerateLanguage)
    
    # Compose GetValue method invocation expression.
    $getValueMethodInvocationExpression = [System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeArgumentReferenceExpression]::new('field'), 'GetValue', @([System.CodeDom.CodePrimitiveExpression]::new($null)))
    
    $sourceCodeBuilder = [System.Text.StringBuilder]::new()
    $writer = [System.IO.StringWriter]::new($sourceCodeBuilder)
    try {
        $provider.GenerateCodeFromExpression($getValueMethodInvocationExpression, $writer, [System.CodeDom.Compiler.CodeGeneratorOptions]::new())
    }
    finally {
        $writer.Close()
    }

    $getValueMethodInvocationCode = $sourceCodeBuilder.ToString()
    
    # Compose member field query predicate.
    $typeReference = Get-TypeFullName $TypeNamespace $TypeName

    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::AccessMask) -bor [System.CodeDom.MemberAttributes]::Private
    $method.Name = 'MemberFieldPredicate'
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([bool])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new([System.Reflection.FieldInfo], 'field'))
    $linqQueryPredicateReturnStatement = [System.CodeDom.CodeMethodReturnStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodePropertyReferenceExpression]::new([System.CodeDom.CodeArgumentReferenceExpression]::new('field'), 'IsPublic'),
            [System.CodeDom.CodeBinaryOperatorType]::BooleanAnd,
            [System.CodeDom.CodeBinaryOperatorExpression]::new(
                [System.CodeDom.CodePropertyReferenceExpression]::new([System.CodeDom.CodeArgumentReferenceExpression]::new('field'), 'IsStatic'),
                [System.CodeDom.CodeBinaryOperatorType]::BooleanAnd,
                [System.CodeDom.CodeBinaryOperatorExpression]::new(
                    [System.CodeDom.CodeSnippetExpression]::new("$getValueMethodInvocationCode is $typeReference memberValue"),
                    [System.CodeDom.CodeBinaryOperatorType]::BooleanAnd,
                    [System.CodeDom.CodeBinaryOperatorExpression]::new(
                        [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeVariableReferenceExpression]::new('memberValue'), '_value'),
                        [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
                        [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'))))))
    [void]$method.Statements.Add($linqQueryPredicateReturnStatement)
    
    return $method
}

function Get-InheritedToStringMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )

    $methodName = 'ToString'
    $memberFieldVariableName = 'memberField'
    $resultVariableName = 'result'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::AccessMask) -bor [System.CodeDom.MemberAttributes]::Public
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::ScopeMask) -bor [System.CodeDom.MemberAttributes]::Override
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([string])

    # Compose member field query arguments.
    $linqQueryEnumerableArgument = [System.CodeDom.CodePropertyReferenceExpression]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), 'GetType', @()), 'GetTypeInfo', @()), 'DeclaredFields')
    $linqQueryPredicateArgument = [System.CodeDom.CodeMethodReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), 'MemberFieldPredicate')

    # Define method statements.
    $linqQueryStatement = [System.CodeDom.CodeVariableDeclarationStatement]::new('var', $memberFieldVariableName, [System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeTypeReferenceExpression]::new([System.Linq.Enumerable]), 'FirstOrDefault', @($linqQueryEnumerableArgument, $linqQueryPredicateArgument)))
    $result1Statement = [System.CodeDom.CodeVariableDeclarationStatement]::new(($PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new('string?'):[System.CodeDom.CodeTypeReference]::new([string])), $resultVariableName, [System.CodeDom.CodePrimitiveExpression]::new($null))
    $result2Statement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeVariableReferenceExpression]::new($memberFieldVariableName),
            [System.CodeDom.CodeBinaryOperatorType]::IdentityInequality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeAssignStatement]::new([System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName), [System.CodeDom.CodePropertyReferenceExpression]::new([System.CodeDom.CodeVariableReferenceExpression]::new($memberFieldVariableName), 'Name'))))
    $result3Statement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeAssignStatement]::new([System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName), [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'))))
    $result4Statement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeAssignStatement]::new([System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName), [System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeBaseReferenceExpression]::new(), $methodName, @()))))
    $result5Statement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeAssignStatement]::new([System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName), [System.CodeDom.CodePropertyReferenceExpression]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), 'GetType', @()), 'FullName'))))
    $result6Statement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeAssignStatement]::new([System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName), [System.CodeDom.CodePropertyReferenceExpression]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), 'GetType'), 'Name'))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName))
    [System.CodeDom.CodeStatement[]]$methodStatements = @($linqQueryStatement, $result1Statement, $result2Statement, $result3Statement, $result4Statement, $result5Statement, $result6Statement, $returnStatement)
    $method.Statements.AddRange($methodStatements)

    return $method
}

function Get-CompareToMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )
    
    # Declare method.
    $methodName = 'CompareTo'
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::AccessMask) -bor [System.CodeDom.MemberAttributes]::Public
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::ScopeMask) -bor [System.CodeDom.MemberAttributes]::Final
    [void]$method.ImplementationTypes.Add([System.CodeDom.CodeTypeReference]::new([System.IComparable]))
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([int])

    $methodParameterName = 'obj'
    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new('object?'):[System.CodeDom.CodeTypeReference]::new([System.Object])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, $methodParameterName))

    # Define method statements.
    $argumentCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeArgumentReferenceExpression]::new($methodParameterName),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.ArgumentNullException]), @([System.CodeDom.CodeSnippetExpression]::new("nameof($methodParameterName)"))))))
    $typeCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeArgumentReferenceExpression]::new($methodParameterName), 'GetType', @()),
            [System.CodeDom.CodeBinaryOperatorType]::IdentityInequality,
            [System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), 'GetType', @())),
        @([System.CodeDom.CodeThrowExceptionStatement]::new(
                [System.CodeDom.CodeObjectCreateExpression]::new(
                    [System.CodeDom.CodeTypeReference]::new([System.ArgumentException]),
                    @([System.CodeDom.CodeSnippetExpression]::new("$""{nameof($methodParameterName)} and this instance are not the same type."""), [System.CodeDom.CodeSnippetExpression]::new("nameof($methodParameterName)"))))))
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.NullReferenceException]), @()))))
    if ($UseNullableReferenceTypes) {
        $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'), $methodName, @([System.CodeDom.CodeArgumentReferenceExpression]::new($methodParameterName))))
    }
    else {
        $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeCastExpression]::new([System.IComparable], [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value')), 'CompareTo', @([System.CodeDom.CodeArgumentReferenceExpression]::new($methodParameterName))))
    }
    [System.CodeDom.CodeStatement[]]$methodStatements = @($argumentCheckStatement, $typeCheckStatement, $nullCheckStatement, $returnStatement)
    $method.Statements.AddRange($methodStatements)

    return $method
}

function Get-InheritedEqualsMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [string]    $TypeNamespace,

        [Parameter(Mandatory)]
        [string]    $TypeName,

        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )
    
    $methodName = 'Equals'
    $methodParameterName = 'obj'
    $typeReference = Get-TypeFullName $TypeNamespace $TypeName
    
    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::AccessMask) -bor [System.CodeDom.MemberAttributes]::Public
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::ScopeMask) -bor [System.CodeDom.MemberAttributes]::Override
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([bool])

    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new('object?'):[System.CodeDom.CodeTypeReference]::new([System.Object])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, $methodParameterName))

    # Define method statements.
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeSnippetExpression]::new("$methodParameterName is $typeReference other"),
            [System.CodeDom.CodeBinaryOperatorType]::BooleanAnd,
            [System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), $methodName, @([System.CodeDom.CodeVariableReferenceExpression]::new('other')))))
    [void]$method.Statements.Add($returnStatement)

    return $method
}

function Get-GetTypeCodeMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]

    $methodName = 'GetTypeCode'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::AccessMask) -bor [System.CodeDom.MemberAttributes]::Public
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::ScopeMask) -bor [System.CodeDom.MemberAttributes]::Final
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([System.TypeCode])
    [void]$method.ImplementationTypes.Add([System.IConvertible])

    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.InvalidOperationException]), @([System.CodeDom.CodePrimitiveExpression]::new('The enumeration type is unknown.'))))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'), $methodName, @()))
    $method.Statements.AddRange(@($nullCheckStatement, $returnStatement))

    return $method
}

function Get-ToBooleanMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )
    
    $methodName = 'ToBoolean'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([bool])
    $method.PrivateImplementationType = [System.CodeDom.CodeTypeReference]::new([System.IConvertible])

    $interfaceType = [System.IFormatProvider]
    $interfaceTypeName = ${interfaceType}?.FullName
    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new("$($interfaceTypeName)?"):[System.CodeDom.CodeTypeReference]::new([System.IFormatProvider])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, 'provider'))

    # Define method statements.
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.InvalidCastException]), @()))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeCastExpression]::new([System.IConvertible], [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value')), $methodName, @([System.CodeDom.CodeArgumentReferenceExpression]::new('provider'))))
    $method.Statements.AddRange(@($nullCheckStatement, $returnStatement))

    return $method
}

function Get-ToByteMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )
    
    $methodName = 'ToByte'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([byte])
    $method.PrivateImplementationType = [System.CodeDom.CodeTypeReference]::new([System.IConvertible])

    $interfaceType = [System.IFormatProvider]
    $interfaceTypeName = ${interfaceType}?.FullName
    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new("$($interfaceTypeName)?"):[System.CodeDom.CodeTypeReference]::new([System.IFormatProvider])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, 'provider'))

    # Define method statements.
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.InvalidCastException]), @()))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeCastExpression]::new([System.IConvertible], [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value')), $methodName, @([System.CodeDom.CodeArgumentReferenceExpression]::new('provider'))))
    $method.Statements.AddRange(@($nullCheckStatement, $returnStatement))

    return $method
}

function Get-ToCharMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )
        
    $methodName = 'ToChar'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([char])
    $method.PrivateImplementationType = [System.CodeDom.CodeTypeReference]::new([System.IConvertible])

    $interfaceType = [System.IFormatProvider]
    $interfaceTypeName = ${interfaceType}?.FullName
    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new("$($interfaceTypeName)?"):[System.CodeDom.CodeTypeReference]::new([System.IFormatProvider])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, 'provider'))

    # Define method statements.
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.InvalidCastException]), @()))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeCastExpression]::new([System.IConvertible], [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value')), $methodName, @([System.CodeDom.CodeArgumentReferenceExpression]::new('provider'))))
    $method.Statements.AddRange(@($nullCheckStatement, $returnStatement))

    return $method
}

function Get-ToDateTimeMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )

    $methodName = 'ToDateTime'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([datetime])
    $method.PrivateImplementationType = [System.CodeDom.CodeTypeReference]::new([System.IConvertible])

    $interfaceType = [System.IFormatProvider]
    $interfaceTypeName = ${interfaceType}?.FullName
    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new("$($interfaceTypeName)?"):[System.CodeDom.CodeTypeReference]::new([System.IFormatProvider])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, 'provider'))

    # Define method statements.
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.InvalidCastException]), @()))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeCastExpression]::new([System.IConvertible], [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value')), $methodName, @([System.CodeDom.CodeArgumentReferenceExpression]::new('provider'))))
    $method.Statements.AddRange(@($nullCheckStatement, $returnStatement))

    return $method
}

function Get-ToDecimalMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )

    $methodName = 'ToDecimal'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([decimal])
    $method.PrivateImplementationType = [System.CodeDom.CodeTypeReference]::new([System.IConvertible])

    $interfaceType = [System.IFormatProvider]
    $interfaceTypeName = ${interfaceType}?.FullName
    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new("$($interfaceTypeName)?"):[System.CodeDom.CodeTypeReference]::new([System.IFormatProvider])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, 'provider'))

    # Define method statements.
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.InvalidCastException]), @()))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeCastExpression]::new([System.IConvertible], [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value')), $methodName, @([System.CodeDom.CodeArgumentReferenceExpression]::new('provider'))))
    $method.Statements.AddRange(@($nullCheckStatement, $returnStatement))

    return $method
}

function Get-ToDoubleMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )

    $methodName = 'ToDouble'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([double])
    $method.PrivateImplementationType = [System.CodeDom.CodeTypeReference]::new([System.IConvertible])

    $interfaceType = [System.IFormatProvider]
    $interfaceTypeName = ${interfaceType}?.FullName
    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new("$($interfaceTypeName)?"):[System.CodeDom.CodeTypeReference]::new([System.IFormatProvider])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, 'provider'))

    # Define method statements.
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.InvalidCastException]), @()))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeCastExpression]::new([System.IConvertible], [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value')), $methodName, @([System.CodeDom.CodeArgumentReferenceExpression]::new('provider'))))
    $method.Statements.AddRange(@($nullCheckStatement, $returnStatement))

    return $method
}

function Get-ToInt16MethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )

    $methodName = 'ToInt16'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([Int16])
    $method.PrivateImplementationType = [System.CodeDom.CodeTypeReference]::new([System.IConvertible])

    $interfaceType = [System.IFormatProvider]
    $interfaceTypeName = ${interfaceType}?.FullName
    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new("$($interfaceTypeName)?"):[System.CodeDom.CodeTypeReference]::new([System.IFormatProvider])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, 'provider'))

    # Define method statements.
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.InvalidCastException]), @()))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeCastExpression]::new([System.IConvertible], [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value')), $methodName, @([System.CodeDom.CodeArgumentReferenceExpression]::new('provider'))))
    $method.Statements.AddRange(@($nullCheckStatement, $returnStatement))

    return $method
}

function Get-ToInt32MethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )

    $methodName = 'ToInt32'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([Int32])
    $method.PrivateImplementationType = [System.CodeDom.CodeTypeReference]::new([System.IConvertible])

    $interfaceType = [System.IFormatProvider]
    $interfaceTypeName = ${interfaceType}?.FullName
    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new("$($interfaceTypeName)?"):[System.CodeDom.CodeTypeReference]::new([System.IFormatProvider])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, 'provider'))

    # Define method statements.
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.InvalidCastException]), @()))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeCastExpression]::new([System.IConvertible], [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value')), $methodName, @([System.CodeDom.CodeArgumentReferenceExpression]::new('provider'))))
    $method.Statements.AddRange(@($nullCheckStatement, $returnStatement))

    return $method
}

function Get-ToInt64MethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )

    $methodName = 'ToInt64'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([Int64])
    $method.PrivateImplementationType = [System.CodeDom.CodeTypeReference]::new([System.IConvertible])

    $interfaceType = [System.IFormatProvider]
    $interfaceTypeName = ${interfaceType}?.FullName
    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new("$($interfaceTypeName)?"):[System.CodeDom.CodeTypeReference]::new([System.IFormatProvider])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, 'provider'))

    # Define method statements.
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.InvalidCastException]), @()))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeCastExpression]::new([System.IConvertible], [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value')), $methodName, @([System.CodeDom.CodeArgumentReferenceExpression]::new('provider'))))
    $method.Statements.AddRange(@($nullCheckStatement, $returnStatement))

    return $method
}

function Get-ToSByteMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )

    $methodName = 'ToSByte'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([sbyte])
    $method.PrivateImplementationType = [System.CodeDom.CodeTypeReference]::new([System.IConvertible])

    $interfaceType = [System.IFormatProvider]
    $interfaceTypeName = ${interfaceType}?.FullName
    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new("$($interfaceTypeName)?"):[System.CodeDom.CodeTypeReference]::new([System.IFormatProvider])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, 'provider'))

    # Define method statements.
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.InvalidCastException]), @()))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeCastExpression]::new([System.IConvertible], [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value')), $methodName, @([System.CodeDom.CodeArgumentReferenceExpression]::new('provider'))))
    $method.Statements.AddRange(@($nullCheckStatement, $returnStatement))

    return $method
}

function Get-ToSingleMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )

    $methodName = 'ToSingle'

    # Define method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([float])
    $method.PrivateImplementationType = [System.CodeDom.CodeTypeReference]::new([System.IConvertible])

    $interfaceType = [System.IFormatProvider]
    $interfaceTypeName = ${interfaceType}?.FullName
    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new("$($interfaceTypeName)?"):[System.CodeDom.CodeTypeReference]::new([System.IFormatProvider])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, 'provider'))

    # Define method statements.
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.InvalidCastException]), @()))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeCastExpression]::new([System.IConvertible], [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value')), $methodName, @([System.CodeDom.CodeArgumentReferenceExpression]::new('provider'))))
    $method.Statements.AddRange(@($nullCheckStatement, $returnStatement))

    return $method
}

function Get-ToStringMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )
                        
    $methodName = 'ToString'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::AccessMask) -bor [System.CodeDom.MemberAttributes]::Public
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::ScopeMask) -bor [System.CodeDom.MemberAttributes]::Final
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([string])
    [void]$method.ImplementationTypes.Add([System.CodeDom.CodeTypeReference]::new([System.IConvertible]))
    [void]$method.CustomAttributes.Add([System.CodeDom.CodeAttributeDeclaration]::new(
            [System.CodeDom.CodeTypeReference]::new([System.ObsoleteAttribute]),
            @([System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodeBinaryOperatorExpression]::new([System.CodeDom.CodePrimitiveExpression]::new('The provider argument is not used. Please use '), [System.CodeDom.CodeBinaryOperatorType]::Add, [System.CodeDom.CodeBinaryOperatorExpression]::new([System.CodeDom.CodeSnippetExpression]::new("nameof($methodName)"), [System.CodeDom.CodeBinaryOperatorType]::Add, [System.CodeDom.CodePrimitiveExpression]::new('().')))))))

    $interfaceType = [System.IFormatProvider]
    $interfaceTypeName = ${interfaceType}?.FullName
    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new("$($interfaceTypeName)?"):[System.CodeDom.CodeTypeReference]::new([System.IFormatProvider])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, 'provider'))

    # Define method statements.
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.InvalidCastException]), @()))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeCastExpression]::new([System.IConvertible], [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value')), $methodName, @([System.CodeDom.CodeArgumentReferenceExpression]::new('provider'))))
    $method.Statements.AddRange(@($nullCheckStatement, $returnStatement))

    return $method
}

function Get-ToTypeMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )

    $methodName = 'ToType'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([System.Object])
    $method.PrivateImplementationType = [System.CodeDom.CodeTypeReference]::new([System.IConvertible])

    $interfaceType = [System.IFormatProvider]
    $interfaceTypeName = ${interfaceType}?.FullName
    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new("$($interfaceTypeName)?"):[System.CodeDom.CodeTypeReference]::new([System.IFormatProvider])
    $method.Parameters.AddRange(@([System.CodeDom.CodeParameterDeclarationExpression]::new([type], 'conversionType'), [System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, 'provider')))

    # Define method statements.
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.InvalidCastException]), @()))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new(
        [System.CodeDom.CodeMethodInvokeExpression]::new(
            [System.CodeDom.CodeCastExpression]::new([System.IConvertible], [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value')), 
            $methodName, 
            @([System.CodeDom.CodeArgumentReferenceExpression]::new('conversionType'), [System.CodeDom.CodeArgumentReferenceExpression]::new('provider'))))
    $method.Statements.AddRange(@($nullCheckStatement, $returnStatement))

    return $method
}

function Get-ToUInt16MethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )

    $methodName = 'ToUInt16'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([UInt16])
    $method.PrivateImplementationType = [System.CodeDom.CodeTypeReference]::new([System.IConvertible])

    $interfaceType = [System.IFormatProvider]
    $interfaceTypeName = ${interfaceType}?.FullName
    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new("$($interfaceTypeName)?"):[System.CodeDom.CodeTypeReference]::new([System.IFormatProvider])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, 'provider'))

    # Define method statements.
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.InvalidCastException]), @()))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeCastExpression]::new([System.IConvertible], [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value')), $methodName, @([System.CodeDom.CodeArgumentReferenceExpression]::new('provider'))))
    $method.Statements.AddRange(@($nullCheckStatement, $returnStatement))

    return $method
}

function Get-ToUInt32MethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )

    $methodName = 'ToUInt32'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([UInt32])
    $method.PrivateImplementationType = [System.CodeDom.CodeTypeReference]::new([System.IConvertible])

    $interfaceType = [System.IFormatProvider]
    $interfaceTypeName = ${interfaceType}?.FullName
    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new("$($interfaceTypeName)?"):[System.CodeDom.CodeTypeReference]::new([System.IFormatProvider])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, 'provider'))

    # Define method statements.
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.InvalidCastException]), @()))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeCastExpression]::new([System.IConvertible], [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value')), $methodName, @([System.CodeDom.CodeArgumentReferenceExpression]::new('provider'))))
    $method.Statements.AddRange(@($nullCheckStatement, $returnStatement))

    return $method
}

function Get-ToUInt64MethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )

    $methodName = 'ToUInt64'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([UInt64])
    $method.PrivateImplementationType = [System.CodeDom.CodeTypeReference]::new([System.IConvertible])

    $interfaceType = [System.IFormatProvider]
    $interfaceTypeName = ${interfaceType}?.FullName
    $methodParameterType = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new("$($interfaceTypeName)?"):[System.CodeDom.CodeTypeReference]::new([System.IFormatProvider])
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new($methodParameterType, 'provider'))

    # Define method statements.
    $nullCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.InvalidCastException]), @()))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeCastExpression]::new([System.IConvertible], [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value')), $methodName, @([System.CodeDom.CodeArgumentReferenceExpression]::new('provider'))))
    [System.CodeDom.CodeStatement[]]$methodStatements = @($nullCheckStatement, $returnStatement)
    $method.Statements.AddRange($methodStatements)

    return $method
}

function Get-TypeFullName {
    [OutputType([string])]
    param (
        [Parameter()]
        [string]    $TypeNamespace,

        [Parameter()]
        [string]    $TypeName
    )
    
    [string[]]$typeReferenceParts = @()
    if (-not [string]::IsNullOrEmpty($TypeNamespace)) {
        $typeReferenceParts += $TypeNamespace
    }
    $typeReferenceParts += $TypeName

    return $typeReferenceParts -join '.'
}

Export-ModuleMember -Function Get-ValueFieldDeclaration
Export-ModuleMember -Function Get-EqualsMethodDefinition
Export-ModuleMember -Function Get-GetHashCodeMethodDefinition
Export-ModuleMember -Function Get-MemberFieldPredicateMethodDefinition
Export-ModuleMember -Function Get-InheritedToStringMethodDefinition
Export-ModuleMember -Function Get-CompareToMethodDefinition
Export-ModuleMember -Function Get-InheritedEqualsMethodDefinition
Export-ModuleMember -Function Get-GetTypeCodeMethodDefinition
Export-ModuleMember -Function Get-ToBooleanMethodDefinition
Export-ModuleMember -Function Get-ToByteMethodDefinition
Export-ModuleMember -Function Get-ToCharMethodDefinition
Export-ModuleMember -Function Get-ToDateTimeMethodDefinition
Export-ModuleMember -Function Get-ToDecimalMethodDefinition
Export-ModuleMember -Function Get-ToDoubleMethodDefinition
Export-ModuleMember -Function Get-ToInt16MethodDefinition
Export-ModuleMember -Function Get-ToInt32MethodDefinition
Export-ModuleMember -Function Get-ToInt64MethodDefinition
Export-ModuleMember -Function Get-ToSByteMethodDefinition
Export-ModuleMember -Function Get-ToSingleMethodDefinition
Export-ModuleMember -Function Get-ToStringMethodDefinition
Export-ModuleMember -Function Get-ToTypeMethodDefinition
Export-ModuleMember -Function Get-ToUInt16MethodDefinition
Export-ModuleMember -Function Get-ToUInt32MethodDefinition
Export-ModuleMember -Function Get-ToUInt64MethodDefinition