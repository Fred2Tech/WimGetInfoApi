# Script de test pour tous les endpoints API WIM
# Créé le 9 septembre 2025

Write-Host "=== TESTS COMPLETS API WIM ===" -ForegroundColor Green
Write-Host ""

# Configuration des chemins de test
$bootWimPath = "D:\Operating Systems\Boot\boot.wim"
$w10WimPath = "d:\Operating Systems\W10-23H2\W10-23H2.wim"
$baseUrl = "http://localhost:8080"

Write-Host "1. Test Health Check" -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health" -Method GET
    Write-Host "   ✅ Status: $($health.status)" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Erreur: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "2. Test Info API" -ForegroundColor Yellow
try {
    $info = Invoke-RestMethod -Uri "$baseUrl/info" -Method GET
    Write-Host "   ✅ Version: $($info.version)" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Erreur: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "3. Test Service Info" -ForegroundColor Yellow
try {
    $serviceInfo = Invoke-RestMethod -Uri "$baseUrl/api/Wim/service-info" -Method GET
    Write-Host "   ✅ Service Type: OK" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Erreur: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "4. Test Check File (Boot.wim)" -ForegroundColor Yellow
try {
    $encodedBootPath = [System.Web.HttpUtility]::UrlEncode($bootWimPath)
    $checkFile = Invoke-RestMethod -Uri "$baseUrl/api/Wim/check-file?wimFilePath=$encodedBootPath" -Method GET
    Write-Host "   ✅ Fichier accessible: $($checkFile.success)" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Erreur: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "5. Test File Info (Boot.wim)" -ForegroundColor Yellow
try {
    $encodedBootPath = [System.Web.HttpUtility]::UrlEncode($bootWimPath)
    $fileInfo = Invoke-RestMethod -Uri "$baseUrl/api/Wim/file-info?wimFilePath=$encodedBootPath" -Method GET
    Write-Host "   ✅ Images: $($fileInfo.data.imageCount)" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Erreur: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "6. Test Image Info (Boot.wim Image 1)" -ForegroundColor Yellow
try {
    $encodedBootPath = [System.Web.HttpUtility]::UrlEncode($bootWimPath)
    $imageInfo = Invoke-RestMethod -Uri "$baseUrl/api/Wim/image-info?wimFilePath=$encodedBootPath&imageIndex=1" -Method GET
    Write-Host "   ✅ Edition: $($imageInfo.data.edition), Languages: $($imageInfo.data.languages)" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Erreur: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "7. Test All Images Info (Boot.wim)" -ForegroundColor Yellow
try {
    $encodedBootPath = [System.Web.HttpUtility]::UrlEncode($bootWimPath)
    $allImages = Invoke-RestMethod -Uri "$baseUrl/api/Wim/all-images-info?wimFilePath=$encodedBootPath" -Method GET
    Write-Host "   ✅ Total images: $($allImages.data.Count)" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Erreur: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "8. Test Analyze POST (Boot.wim)" -ForegroundColor Yellow
try {
    $analyzeBody = @{
        wimFilePath = $bootWimPath
        imageIndex = "1"
    } | ConvertTo-Json
    
    $analyzeResult = Invoke-RestMethod -Uri "$baseUrl/api/Wim/analyze" -Method POST -ContentType "application/json" -Body $analyzeBody
    Write-Host "   ✅ Edition: $($analyzeResult.data.edition), Languages: $($analyzeResult.data.languages)" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Erreur: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "9. Test Debug XML (Boot.wim)" -ForegroundColor Yellow
try {
    $encodedBootPath = [System.Web.HttpUtility]::UrlEncode($bootWimPath)
    $debugXml = Invoke-RestMethod -Uri "$baseUrl/api/Wim/debug-xml?wimFilePath=$encodedBootPath" -Method GET
    Write-Host "   ✅ XML data récupéré" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Erreur: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== TESTS TERMINÉS ===" -ForegroundColor Green
Write-Host "Pour des tests plus avancés, utilisez les exemples individuels ci-dessus."
