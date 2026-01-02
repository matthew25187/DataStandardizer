function ConvertFrom-Base64String {
    param (
        # Input.
        [Parameter(Mandatory, ValueFromPipeline)]
        [string]    $InputObject
    )
    
    process {
        $inputBytes = [System.Convert]::FromBase64String($InputObject)
        return [System.Text.Encoding]::UTF8.GetString($inputBytes)
    }
}

function ConvertTo-Base64String {
    param (
        # Input.
        [Parameter(Mandatory, ValueFromPipeline)]
        [string]    $InputObject
    )
    
    process {
        $inputBytes = [System.Text.Encoding]::UTF8.GetBytes($InputObject)
        return [System.Convert]::ToBase64String($inputBytes)
    }
}