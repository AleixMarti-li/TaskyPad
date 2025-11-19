# TaskyPad Log Analyzer
# Este script ayuda a analizar los logs de TaskyPad para diagnosticar problemas

param(
    [string]$LogDate = (Get-Date -Format "yyyyMMdd"),
    [switch]$ShowErrors,
    [switch]$ShowWindowEvents,
    [switch]$ShowStartup,
    [switch]$ShowAll,
    [switch]$Latest
)

$logsPath = "$env:LOCALAPPDATA\TaskyPad\Logs"

Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "    TaskyPad Log Analyzer" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Verificar que existe la carpeta de logs
if (-not (Test-Path $logsPath)) {
    Write-Host "? No se encontró la carpeta de logs: $logsPath" -ForegroundColor Red
    exit 1
}

# Obtener el archivo de log
$logFiles = Get-ChildItem $logsPath -Filter "taskypad_*.log" | Sort-Object LastWriteTime -Descending

if ($logFiles.Count -eq 0) {
    Write-Host "? No se encontraron archivos de log en: $logsPath" -ForegroundColor Red
    exit 1
}

# Seleccionar archivo de log
if ($Latest) {
    $logFile = $logFiles[0]
    Write-Host "?? Analizando el log más reciente:" -ForegroundColor Green
} else {
    $logFile = $logFiles | Where-Object { $_.Name -like "taskypad_$LogDate*.log" } | Select-Object -First 1
    if (-not $logFile) {
        Write-Host "? No se encontró log para la fecha: $LogDate" -ForegroundColor Red
        Write-Host ""
        Write-Host "Logs disponibles:" -ForegroundColor Yellow
        $logFiles | Select-Object -First 5 | ForEach-Object {
            Write-Host "  - $($_.Name) ($($_.LastWriteTime))" -ForegroundColor Gray
        }
        exit 1
    }
    Write-Host "?? Analizando log de la fecha: $LogDate" -ForegroundColor Green
}

Write-Host "   Archivo: $($logFile.Name)" -ForegroundColor Gray
Write-Host "   Tamaño: $([math]::Round($logFile.Length / 1KB, 2)) KB" -ForegroundColor Gray
Write-Host "   Última modificación: $($logFile.LastWriteTime)" -ForegroundColor Gray
Write-Host ""

$logContent = Get-Content $logFile.FullName

# Función para mostrar líneas con color
function Show-LogLines {
    param(
        [string[]]$Lines,
        [string]$Title,
        [string]$Color = "White"
    )
    
    if ($Lines.Count -gt 0) {
        Write-Host ""
        Write-Host "?????????????????????????????????????????????????????" -ForegroundColor DarkGray
        Write-Host "  $Title" -ForegroundColor $Color
        Write-Host "?????????????????????????????????????????????????????" -ForegroundColor DarkGray
        $Lines | ForEach-Object {
            if ($_ -match '\[ERR\]|\[FAT\]') {
                Write-Host $_ -ForegroundColor Red
            } elseif ($_ -match '\[WRN\]') {
                Write-Host $_ -ForegroundColor Yellow
            } elseif ($_ -match '\[INF\]') {
                Write-Host $_ -ForegroundColor White
            } elseif ($_ -match '\[DBG\]') {
                Write-Host $_ -ForegroundColor Gray
            } else {
                Write-Host $_ -ForegroundColor Gray
            }
        }
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "  ? No se encontraron registros de: $Title" -ForegroundColor Green
        Write-Host ""
    }
}

# Mostrar errores
if ($ShowErrors -or $ShowAll) {
    $errors = $logContent | Select-String -Pattern '\[ERR\]|\[FAT\]'
    Show-LogLines -Lines $errors -Title "ERRORES Y FALLOS CRÍTICOS" -Color "Red"
    
    $warnings = $logContent | Select-String -Pattern '\[WRN\]'
    Show-LogLines -Lines $warnings -Title "ADVERTENCIAS" -Color "Yellow"
}

# Mostrar eventos de inicio
if ($ShowStartup -or $ShowAll) {
    $startupEvents = $logContent | Select-String -Pattern 'Application Started|OnStartup|MODE DETECTED|MainWindow constructor'
    Show-LogLines -Lines $startupEvents -Title "EVENTOS DE INICIO" -Color "Cyan"
}

# Mostrar eventos de ventana
if ($ShowWindowEvents -or $ShowAll) {
    $windowEvents = $logContent | Select-String -Pattern 'MainWindow|WindowState|Visibility|TrayIcon|OnClosing|OnActivated|ShowInTaskbar'
    Show-LogLines -Lines $windowEvents -Title "EVENTOS DE VENTANA" -Color "Magenta"
}

# Resumen general
Write-Host ""
Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "    RESUMEN" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Cyan

$totalLines = $logContent.Count
$errorsCount = ($logContent | Select-String -Pattern '\[ERR\]').Count
$fatalCount = ($logContent | Select-String -Pattern '\[FAT\]').Count
$warningsCount = ($logContent | Select-String -Pattern '\[WRN\]').Count
$infoCount = ($logContent | Select-String -Pattern '\[INF\]').Count

Write-Host "Líneas totales: $totalLines" -ForegroundColor White
Write-Host "Errores fatales: $fatalCount" -ForegroundColor $(if ($fatalCount -gt 0) { "Red" } else { "Green" })
Write-Host "Errores: $errorsCount" -ForegroundColor $(if ($errorsCount -gt 0) { "Red" } else { "Green" })
Write-Host "Advertencias: $warningsCount" -ForegroundColor $(if ($warningsCount -gt 0) { "Yellow" } else { "Green" })
Write-Host "Información: $infoCount" -ForegroundColor White

# Detectar problemas comunes
Write-Host ""
Write-Host "?????????????????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host "  DIAGNÓSTICO AUTOMÁTICO" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????" -ForegroundColor DarkGray

# ¿Inició en modo silencioso?
if ($logContent | Select-String -Pattern "SILENT MODE DETECTED") {
    Write-Host "? La aplicación inició en MODO SILENCIOSO" -ForegroundColor Yellow
    Write-Host "  Esto significa que la ventana no se mostrará automáticamente." -ForegroundColor Gray
} else {
    Write-Host "? La aplicación inició en MODO NORMAL" -ForegroundColor Green
}

# ¿Hay múltiples instancias?
if ($logContent | Select-String -Pattern "Another instance is already running") {
    Write-Host "? Se detectó intento de abrir múltiples instancias" -ForegroundColor Yellow
} else {
    Write-Host "? No se detectaron conflictos de múltiples instancias" -ForegroundColor Green
}

# ¿Se renderizó la ventana?
if ($logContent | Select-String -Pattern "OnContentRendered") {
    Write-Host "? La ventana se renderizó correctamente" -ForegroundColor Green
} else {
    Write-Host "? La ventana NO se renderizó" -ForegroundColor Red
    Write-Host "  Esto puede indicar un problema crítico en la inicialización." -ForegroundColor Gray
}

# ¿Se activó la ventana?
if ($logContent | Select-String -Pattern "MainWindow activated") {
    Write-Host "? La ventana se activó correctamente" -ForegroundColor Green
} else {
    Write-Host "? La ventana no se activó" -ForegroundColor Yellow
}

# ¿Hay errores de cifrado?
if ($logContent | Select-String -Pattern "Failed to decrypt|Error al descifrar") {
    Write-Host "? Se detectaron errores de CIFRADO/DESCIFRADO" -ForegroundColor Red
    Write-Host "  Revisa la contraseña de cifrado en la configuración." -ForegroundColor Gray
}

# ¿Hay errores en la carga de tareas?
if ($logContent | Select-String -Pattern "Error deserializing tasks|Failed to load tasks") {
    Write-Host "? Se detectaron errores al cargar TAREAS" -ForegroundColor Red
}

Write-Host ""
Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Ayuda
if (-not ($ShowErrors -or $ShowWindowEvents -or $ShowStartup -or $ShowAll)) {
    Write-Host "Uso del script:" -ForegroundColor Yellow
    Write-Host "  .\Analyze-TaskyPadLogs.ps1 -Latest              # Analizar el log más reciente" -ForegroundColor Gray
    Write-Host "  .\Analyze-TaskyPadLogs.ps1 -LogDate 20250119    # Analizar log de fecha específica" -ForegroundColor Gray
    Write-Host "  .\Analyze-TaskyPadLogs.ps1 -ShowErrors          # Mostrar solo errores" -ForegroundColor Gray
    Write-Host "  .\Analyze-TaskyPadLogs.ps1 -ShowStartup         # Mostrar eventos de inicio" -ForegroundColor Gray
    Write-Host "  .\Analyze-TaskyPadLogs.ps1 -ShowWindowEvents    # Mostrar eventos de ventana" -ForegroundColor Gray
    Write-Host "  .\Analyze-TaskyPadLogs.ps1 -ShowAll             # Mostrar todo" -ForegroundColor Gray
    Write-Host ""
}
