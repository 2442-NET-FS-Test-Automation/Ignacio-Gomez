Add-Type -AssemblyName System.Drawing

$outputPath = Join-Path (Get-Location) "docs\bloomrush-relationship-schema.png"

$width = 2200
$height = 1350
$bitmap = New-Object System.Drawing.Bitmap $width, $height
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

$colors = @{
    Background = [System.Drawing.Color]::FromArgb(249, 250, 251)
    Text = [System.Drawing.Color]::FromArgb(17, 24, 39)
    Muted = [System.Drawing.Color]::FromArgb(75, 85, 99)
    Border = [System.Drawing.Color]::FromArgb(148, 163, 184)
    HeaderA = [System.Drawing.Color]::FromArgb(15, 118, 110)
    HeaderB = [System.Drawing.Color]::FromArgb(14, 116, 144)
    HeaderC = [System.Drawing.Color]::FromArgb(124, 58, 237)
    RowAlt = [System.Drawing.Color]::FromArgb(241, 245, 249)
    Line = [System.Drawing.Color]::FromArgb(51, 65, 85)
}

$backgroundBrush = New-Object System.Drawing.SolidBrush $colors.Background
$textBrush = New-Object System.Drawing.SolidBrush $colors.Text
$mutedBrush = New-Object System.Drawing.SolidBrush $colors.Muted
$whiteBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
$tableBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
$rowAltBrush = New-Object System.Drawing.SolidBrush $colors.RowAlt
$borderPen = New-Object System.Drawing.Pen $colors.Border, 2
$linePen = New-Object System.Drawing.Pen $colors.Line, 3
$lightPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(226, 232, 240)), 1

$titleFont = New-Object System.Drawing.Font "Segoe UI", 34, ([System.Drawing.FontStyle]::Bold)
$subtitleFont = New-Object System.Drawing.Font "Segoe UI", 15, ([System.Drawing.FontStyle]::Regular)
$tableFont = New-Object System.Drawing.Font "Segoe UI", 18, ([System.Drawing.FontStyle]::Bold)
$fieldFont = New-Object System.Drawing.Font "Consolas", 12, ([System.Drawing.FontStyle]::Regular)
$labelFont = New-Object System.Drawing.Font "Segoe UI", 13, ([System.Drawing.FontStyle]::Bold)
$legendFont = New-Object System.Drawing.Font "Segoe UI", 12, ([System.Drawing.FontStyle]::Regular)

$center = New-Object System.Drawing.StringFormat
$center.Alignment = [System.Drawing.StringAlignment]::Center
$center.LineAlignment = [System.Drawing.StringAlignment]::Center

$left = New-Object System.Drawing.StringFormat
$left.Alignment = [System.Drawing.StringAlignment]::Near
$left.LineAlignment = [System.Drawing.StringAlignment]::Center

function New-RectF {
    param([int]$X, [int]$Y, [int]$W, [int]$H)
    return New-Object System.Drawing.RectangleF ([single]$X), ([single]$Y), ([single]$W), ([single]$H)
}

function Draw-Table {
    param(
        [string]$Name,
        [string[]]$Fields,
        [int]$X,
        [int]$Y,
        [int]$W,
        [System.Drawing.Color]$HeaderColor
    )

    $headerHeight = 56
    $rowHeight = 34
    $height = $headerHeight + ($Fields.Count * $rowHeight)
    $rect = New-Object System.Drawing.Rectangle $X, $Y, $W, $height

    $graphics.FillRectangle($tableBrush, $rect)
    $graphics.DrawRectangle($borderPen, $rect)

    $headerBrush = New-Object System.Drawing.SolidBrush $HeaderColor
    $graphics.FillRectangle($headerBrush, (New-Object System.Drawing.Rectangle $X, $Y, $W, $headerHeight))
    $graphics.DrawString($Name, $tableFont, $whiteBrush, (New-RectF $X $Y $W $headerHeight), $center)
    $headerBrush.Dispose()

    for ($i = 0; $i -lt $Fields.Count; $i++) {
        $rowY = $Y + $headerHeight + ($i * $rowHeight)
        if ($i % 2 -eq 1) {
            $graphics.FillRectangle($rowAltBrush, (New-Object System.Drawing.Rectangle $X, $rowY, $W, $rowHeight))
        }
        $graphics.DrawLine($lightPen, $X, $rowY, $X + $W, $rowY)
        $graphics.DrawString($Fields[$i], $fieldFont, $textBrush, (New-RectF ($X + 16) $rowY ($W - 32) $rowHeight), $left)
    }

    return @{
        Left = [System.Drawing.Point]::new($X, [int]($Y + $height / 2))
        Right = [System.Drawing.Point]::new($X + $W, [int]($Y + $height / 2))
        Top = [System.Drawing.Point]::new([int]($X + $W / 2), $Y)
        Bottom = [System.Drawing.Point]::new([int]($X + $W / 2), $Y + $height)
    }
}

function Draw-Relation {
    param(
        [System.Drawing.Point[]]$Points,
        [string]$Cardinality,
        [string]$Caption,
        [int]$LabelX,
        [int]$LabelY,
        [int]$CaptionX,
        [int]$CaptionY
    )

    for ($i = 0; $i -lt $Points.Count - 1; $i++) {
        $graphics.DrawLine($linePen, $Points[$i], $Points[$i + 1])
    }

    $badgeRect = New-Object System.Drawing.Rectangle $LabelX, $LabelY, 70, 30
    $captionRect = New-RectF $CaptionX $CaptionY 280 24

    $badgeBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $badgePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(203, 213, 225)), 1
    $graphics.FillRectangle($badgeBrush, $badgeRect)
    $graphics.DrawRectangle($badgePen, $badgeRect)

    $graphics.DrawString($Cardinality, $labelFont, $textBrush, (New-RectF $badgeRect.X $badgeRect.Y $badgeRect.Width $badgeRect.Height), $center)
    if (-not [string]::IsNullOrWhiteSpace($Caption)) {
        $graphics.DrawString($Caption, $legendFont, $mutedBrush, $captionRect, $center)
    }

    $badgeBrush.Dispose()
    $badgePen.Dispose()
}

$graphics.FillRectangle($backgroundBrush, 0, 0, $width, $height)
$graphics.DrawString("BloomRush Database Relationships", $titleFont, $textBrush, 30, 30)
$graphics.DrawString("Cardinalities are placed beside each relationship. SQL-style field types are shown inside each table.", $subtitleFont, $mutedBrush, 32, 92)

$customers = Draw-Table "Customers" @(
    "PK Id int",
    "Name nvarchar(100)",
    "Email nvarchar(255) UK"
) 80 210 500 $colors.HeaderA

$orders = Draw-Table "Orders" @(
    "PK Id int",
    "FK CustomerId int",
    "Priority int enum",
    "Status int enum",
    "CreatedAtUtc datetime2",
    "CompletedAtUtc datetime2 null"
) 80 560 500 $colors.HeaderB

$orderLines = Draw-Table "OrderLines" @(
    "PK Id int",
    "FK OrderId int",
    "FK ProductId int",
    "Quantity int"
) 850 560 500 $colors.HeaderC

$products = Draw-Table "Products" @(
    "PK Id int",
    "Sku nvarchar(100) UK",
    "Name nvarchar(120)",
    "Price decimal(10,2)"
) 1620 210 500 $colors.HeaderA

$inventory = Draw-Table "InventoryItems" @(
    "PK Id int",
    "FK ProductId int UK",
    "QuantityOnHand int",
    "RowVersion rowversion"
) 1620 585 500 $colors.HeaderB

$events = Draw-Table "FulfillmentEvents" @(
    "PK Id int",
    "FK OrderId int",
    "Type int enum",
    "Message nvarchar(500)",
    "TimestampUtc datetime2"
) 850 930 580 $colors.HeaderA

Draw-Relation @(
    $customers.Bottom,
    [System.Drawing.Point]::new($customers.Bottom.X, 525),
    $orders.Top
) "1:N" "" 605 395 605 430

Draw-Relation @(
    $orders.Right,
    [System.Drawing.Point]::new(720, $orders.Right.Y),
    [System.Drawing.Point]::new(720, $orderLines.Left.Y),
    $orderLines.Left
) "1:N" "" 625 645 600 685

Draw-Relation @(
    $products.Left,
    [System.Drawing.Point]::new(1495, $products.Left.Y),
    [System.Drawing.Point]::new(1495, $orderLines.Right.Y),
    $orderLines.Right
) "1:N" "" 1395 500 1360 535

Draw-Relation @(
    $products.Bottom,
    [System.Drawing.Point]::new($products.Bottom.X, 535),
    $inventory.Top
) "1:1" "" 1945 470 1930 480

Draw-Relation @(
    $orders.Bottom,
    [System.Drawing.Point]::new($orders.Bottom.X, 1130),
    [System.Drawing.Point]::new(760, 1130),
    [System.Drawing.Point]::new(760, $events.Left.Y),
    $events.Left
) "1:N" "" 575 1025 590 1115

$noteRect = New-Object System.Drawing.Rectangle 80, 1200, 2040, 118
$noteBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(239, 246, 255))
$notePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(147, 197, 253)), 2
$graphics.FillRectangle($noteBrush, $noteRect)
$graphics.DrawRectangle($notePen, $noteRect)
$legendText = "Relationships: Customers 1:N Orders | Orders 1:N OrderLines | Products 1:N OrderLines | Products 1:1 InventoryItems | Orders 1:N FulfillmentEvents`nDerived relation: Orders N:N Products through OrderLines."
$graphics.DrawString($legendText, $legendFont, $textBrush, (New-RectF 95 1210 2010 96), $center)

$bitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)

$noteBrush.Dispose()
$notePen.Dispose()
$graphics.Dispose()
$bitmap.Dispose()

Write-Output $outputPath
