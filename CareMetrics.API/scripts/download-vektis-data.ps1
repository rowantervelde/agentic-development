<#
PowerShell helper to download all available Vektis open‑data CSV files.
Usage from repo root:
    pwsh -File CareMetrics.API\scripts\download-vektis-data.ps1

Downloaded files go to `CareMetrics.API/Data/vektis/` organized by year and
level (postcode3/municipality).  Existing files are skipped so you can re-run
without re-downloading.
#>

$base = Join-Path $PSScriptRoot "..\Data\vektis"
New-Item -ItemType Directory -Path $base -Force | Out-Null

# define URLs explicitly – mirrors data discovered on the website February 2026
$urls = @(
    # postcode3 level
    @{ Year=2023; Level="postcode3"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/2023/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202023%20-%20postcode3.csv" }
    @{ Year=2022; Level="postcode3"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/2022/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202022%20-%20postcode3.csv" }
    @{ Year=2021; Level="postcode3"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/2021/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202021%20-%20postcode3.csv" }
    @{ Year=2020; Level="postcode3"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/2020/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202020%20-%20postcode3_versie2.csv" }
    @{ Year=2019; Level="postcode3"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/2019/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202019%20-%20postcode3.csv" }
    @{ Year=2018; Level="postcode3"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/2018/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202018%20-%20postcode3.csv" }
    @{ Year=2017; Level="postcode3"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/2017/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202017%20-%20postcode3.csv" }
    @{ Year=2016; Level="postcode3"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/Oud/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202016%20-%20postcode3.csv" }
    @{ Year=2015; Level="postcode3"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/Oud/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202015%20-%20postcode3.csv" }
    @{ Year=2014; Level="postcode3"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/Oud/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202014%20-%20postcode3.csv" }
    @{ Year=2013; Level="postcode3"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/Oud/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202013%20-%20postcode3.csv" }
    @{ Year=2012; Level="postcode3"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/Oud/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202012%20-%20postcode3.csv" }
    @{ Year=2011; Level="postcode3"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/Oud/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202011%20-%20postcode3.csv" }
    # gemeenteniveau
    @{ Year=2023; Level="gemeente"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/2023/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202023%20-%20gemeente.csv" }
    @{ Year=2022; Level="gemeente"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/2022/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202022%20-%20gemeente.csv" }
    @{ Year=2021; Level="gemeente"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/2021/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202021%20-%20gemeente.csv" }
    @{ Year=2020; Level="gemeente"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/2020/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202020%20-%20gemeente_versie2.csv" }
    @{ Year=2019; Level="gemeente"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/2019/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202019%20-%20gemeente.csv" }
    @{ Year=2018; Level="gemeente"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/2018/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202018%20-%20gemeente.csv" }
    @{ Year=2017; Level="gemeente"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/2017/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202017%20-%20gemeente.csv" }
    @{ Year=2016; Level="gemeente"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/Gemeentebestanden%202011-2016/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202016%20-%20gemeente.csv" }
    @{ Year=2015; Level="gemeente"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/Gemeentebestanden%202011-2016/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202015%20-%20gemeente.csv" }
    @{ Year=2014; Level="gemeente"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/Gemeentebestanden%202011-2016/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202014%20-%20gemeente.csv" }
    @{ Year=2013; Level="gemeente"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/Gemeentebestanden%202011-2016/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202013%20-%20gemeente.csv" }
    @{ Year=2012; Level="gemeente"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/Gemeentebestanden%202011-2016/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202012%20-%20gemeente.csv" }
    @{ Year=2011; Level="gemeente"; Url="https://www.vektis.nl/uploads/Docs%20per%20pagina/Open%20Data%20Bestanden/Gemeentebestanden%202011-2016/Vektis%20Open%20Databestand%20Zorgverzekeringswet%202011%20-%20gemeente.csv" }
)

foreach ($item in $urls) {
    $year = $item.Year
    $level = $item.Level
    $url = $item.Url
    $outDir = Join-Path $base "$level"
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    $fileName = "vektis_$year`_$level.csv"
    $outPath = Join-Path $outDir $fileName

    if (Test-Path $outPath) {
        Write-Host "Skipping $fileName (already exists)"
        continue
    }

    Write-Host "Downloading $year $level ..."
    try {
        Invoke-WebRequest -Uri $url -OutFile $outPath -UseBasicParsing
        Write-Host "  saved to $outPath"
    } catch {
        Write-Warning "Failed to download ${url}: $_"
    }
}

Write-Host "Downloaded $((Get-ChildItem -Recurse $base | Measure-Object).Count) files."
