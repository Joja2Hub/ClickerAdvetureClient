param(
    [Parameter(Mandatory = $true)]
    [string]$DocumentPath,
    [Parameter(Mandatory = $true)]
    [string]$PdfPath
)

$word = $null
$document = $null
try {
    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = 0

    $document = $word.Documents.Open($DocumentPath)
    $document.Repaginate()

    foreach ($toc in $document.TablesOfContents) {
        foreach ($field in $toc.Range.Fields) {
            $field.Locked = $false
        }
        $toc.Update()
    }
    foreach ($field in $document.Fields) {
        $field.Update() | Out-Null
    }

    $document.Repaginate()
    foreach ($toc in $document.TablesOfContents) {
        $toc.Range.Font.AllCaps = 0
        $encodedReplacements = @(
            @("0KDQldCk0JXQoNCQ0KI=", "0KDQtdGE0LXRgNCw0YI="),
            @("0KHQntCU0JXQoNCW0JDQndCY0JU=", "0KHQvtC00LXRgNC20LDQvdC40LU="),
            @("0JLQktCV0JTQldCd0JjQlQ==", "0JLQstC10LTQtdC90LjQtQ=="),
            @("0JfQkNCa0JvQrtCn0JXQndCY0JU=", "0JfQsNC60LvRjtGH0LXQvdC40LU="),
            @(
                "0KHQn9CY0KHQntCaINCY0KHQn9Ce0JvQrNCX0J7QktCQ0J3QndCr0KUg0JjQodCi0J7Qp9Cd0JjQmtCe0JI=",
                "0KHQv9C40YHQvtC6INC40YHQv9C+0LvRjNC30L7QstCw0L3QvdGL0YUg0LjRgdGC0L7Rh9C90LjQutC+0LI="
            )
        )
        foreach ($pair in $encodedReplacements) {
            $source = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($pair[0]))
            $replacement = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($pair[1]))
            $range = $toc.Range.Duplicate
            $range.Find.ClearFormatting()
            $range.Find.Replacement.ClearFormatting()
            $range.Find.Execute($source, $false, $true, $false, $false, $false, $true, 1, $false, $replacement, 2) | Out-Null
        }
        foreach ($field in $toc.Range.Fields) {
            $field.Locked = $true
        }
    }
    $document.Save()
    $document.ExportAsFixedFormat($PdfPath, 17)
}
finally {
    if ($document -ne $null) {
        $document.Close($false)
        [System.Runtime.InteropServices.Marshal]::ReleaseComObject($document) | Out-Null
    }
    if ($word -ne $null) {
        $word.Quit()
        [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
    }
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}
