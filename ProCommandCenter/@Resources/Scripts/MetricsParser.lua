local function readAll(path)
    local file = io.open(path, "r")
    if not file then
        return nil
    end

    local content = file:read("*a")
    file:close()
    return content
end

local function numberFrom(block, key)
    if not block then
        return nil
    end

    local value = block:match('"' .. key .. '"%s*:%s*([%-]?%d+%.?%d*)')
    if value then
        return tonumber(value)
    end

    return nil
end

local function stringFrom(block, key)
    if not block then
        return nil
    end

    return block:match('"' .. key .. '"%s*:%s*"(.-)"')
end

local function setVar(name, value)
    SKIN:Bang("!SetVariable", name, tostring(value or ""))
end

local function formatNumber(value, decimals, suffix)
    if value == nil then
        return "N/A"
    end

    local fmt = "%0." .. tostring(decimals or 0) .. "f"
    local rendered = string.format(fmt, value)
    return suffix and (rendered .. suffix) or rendered
end

local function formatBytesPerSec(value)
    if value == nil then
        return "N/A"
    end

    local abs = math.abs(value)
    if abs >= 1024 * 1024 then
        return string.format("%.2f MB/s", value / (1024 * 1024))
    elseif abs >= 1024 then
        return string.format("%.1f KB/s", value / 1024)
    end

    return string.format("%.0f B/s", value)
end


local function clampPercent(value)
    if value == nil then
        return 0
    end

    if value < 0 then
        return 0
    end

    if value > 100 then
        return 100
    end

    return value
end

local function formatStorageFromGB(value)
    if value == nil then
        return "N/A"
    end

    if value >= 1024 then
        return string.format("%.2f TB", value / 1024)
    end

    return string.format("%.0f GB", value)
end

local function parseNamedValueArray(arrayText)
    local items = {}
    if not arrayText then
        return items
    end

    for item in arrayText:gmatch("%b{}") do
        table.insert(items, {
            name = stringFrom(item, "name") or "Unknown",
            value = numberFrom(item, "value")
        })
    end

    return items
end

local function coreSortKey(name)
    local n = tonumber((name or ""):match("(%d+)$"))
    return n or 9999
end

local function simplifyFanName(name, index)
    if not name or name == "" then
        return "Fan " .. tostring(index)
    end

    local simplified = name:match("%-%s*(.+)$")
    if simplified and simplified ~= "" then
        return simplified
    end

    return name
end

local function setDiskVisibility(index, visible)
    local hidden = visible and "0" or "1"
    SKIN:Bang("!SetOption", "MeterDisk" .. index, "Hidden", hidden)
    SKIN:Bang("!SetOption", "MeterDisk" .. index .. "BarBack", "Hidden", hidden)
    SKIN:Bang("!SetOption", "MeterDisk" .. index .. "Bar", "Hidden", hidden)
end

local function resetDefaults()
    setVar("CpuUsage", "N/A")
    setVar("CpuTemp", "N/A")
    setVar("CpuCoreLinesLeft", "No per-core usage metrics")
    setVar("CpuCoreLinesRight", "")

    setVar("GpuName", "NVIDIA GeForce RTX 4080 SUPER")
    setVar("GpuUsage", "N/A")
    setVar("GpuTemp", "N/A")

    setVar("MemoryLine", "N/A")
    setVar("MemoryPercent", "0")

    for i = 1, 4 do
        setVar("Disk" .. i .. "Line", "")
        setVar("Disk" .. i .. "Percent", "0")
        setDiskVisibility(i, false)
    end

    setVar("NetworkLine", "Ethernet: DL N/A | UL N/A")

    for i = 1, 8 do
        setVar("Fan" .. i .. "Name", "")
        setVar("Fan" .. i .. "Rpm", "0")
    end
end

local function parseCpu(cpuBlock)
    local usage = numberFrom(cpuBlock, "usageTotalPercent")
    local temp = numberFrom(cpuBlock, "temperatureC")

    setVar("CpuUsage", formatNumber(usage, 0, "%"))
    setVar("CpuTemp", formatNumber(temp, 0, " C"))

    local usages = parseNamedValueArray(cpuBlock and cpuBlock:match('"perCoreUsagePercent"%s*:%s*(%b[])'))
    table.sort(usages, function(a, b)
        local ka = coreSortKey(a.name)
        local kb = coreSortKey(b.name)
        if ka == kb then
            return (a.name or "") < (b.name or "")
        end
        return ka < kb
    end)

    local maxLines = tonumber(SKIN:ReplaceVariables("#MaxCoreLines#")) or 16
    local lines = {}
    for i, c in ipairs(usages) do
        lines[#lines + 1] = string.format("%s: %s", c.name, formatNumber(c.value, 0, "%"))
        if i >= maxLines then
            break
        end
    end

    if #lines == 0 then
        setVar("CpuCoreLinesLeft", "No per-core usage metrics")
        setVar("CpuCoreLinesRight", "")
        return
    end

    local left, right = {}, {}
    local splitAt = math.ceil(#lines / 2)
    for i, line in ipairs(lines) do
        if i <= splitAt then
            left[#left + 1] = line
        else
            right[#right + 1] = line
        end
    end

    setVar("CpuCoreLinesLeft", table.concat(left, "#CRLF#"))
    setVar("CpuCoreLinesRight", table.concat(right, "#CRLF#"))
end

local function parseGpu(allText)
    local gpuArray = allText:match('"gpu"%s*:%s*(%b[])')
    local targetName = "NVIDIA GeForce RTX 4080 SUPER"
    local selected
    local fallback

    if gpuArray then
        for obj in gpuArray:gmatch("%b{}") do
            local name = stringFrom(obj, "name")
            if name == targetName then
                selected = obj
                break
            end
            if not fallback and name and name:find("NVIDIA GeForce RTX 4080", 1, true) then
                fallback = obj
            end
        end
    end

    selected = selected or fallback
    if not selected then
        return
    end

    setVar("GpuName", stringFrom(selected, "name") or targetName)
    setVar("GpuUsage", formatNumber(numberFrom(selected, "coreUsagePercent"), 0, "%"))
    setVar("GpuTemp", formatNumber(numberFrom(selected, "temperatureC"), 0, " C"))
end

local function parseMemory(memoryBlock)
    local total = numberFrom(memoryBlock, "totalMB")
    local used = numberFrom(memoryBlock, "usedMB")
    local available = numberFrom(memoryBlock, "availableMB")
    local percent = numberFrom(memoryBlock, "usagePercent")

    setVar("MemoryLine", string.format(
        "Total %s GB | Used %s GB | Free %s GB | %s",
        formatNumber(total and (total / 1024), 1, ""),
        formatNumber(used and (used / 1024), 1, ""),
        formatNumber(available and (available / 1024), 1, ""),
        formatNumber(percent, 0, "%")
    ))
    setVar("MemoryPercent", clampPercent(percent))
end

local function parseDisks(allText)
    local diskArray = allText:match('"disks"%s*:%s*(%b[])')
    local index = 0

    if diskArray then
        for obj in diskArray:gmatch("%b{}") do
            if index >= 4 then
                break
            end

            index = index + 1
            local name = stringFrom(obj, "name") or stringFrom(obj, "mountPoint") or "Disk"
            local total = numberFrom(obj, "totalGB")
            local used = numberFrom(obj, "usedGB")
            local free = numberFrom(obj, "freeGB")
            local percent = numberFrom(obj, "usagePercent")

            setVar("Disk" .. index .. "Line", string.format(
                "%s | T %s U %s F %s | %s",
                name,
                formatStorageFromGB(total),
                formatStorageFromGB(used),
                formatStorageFromGB(free),
                formatNumber(percent, 0, "%")
            ))
            setVar("Disk" .. index .. "Percent", clampPercent(percent))
            setDiskVisibility(index, true)
        end
    end

    for i = index + 1, 4 do
        setVar("Disk" .. i .. "Line", "")
        setVar("Disk" .. i .. "Percent", "0")
        setDiskVisibility(i, false)
    end
end

local function parseNetwork(allText)
    local netArray = allText:match('"network"%s*:%s*(%b[])')
    if not netArray then
        return
    end

    local selected
    for obj in netArray:gmatch("%b{}") do
        local name = stringFrom(obj, "name")
        if name == "Ethernet" then
            selected = obj
            break
        end
    end

    if not selected then
        return
    end

    local up = numberFrom(selected, "bytesSentPerSec")
    local down = numberFrom(selected, "bytesReceivedPerSec")
    setVar("NetworkLine", string.format("Ethernet: DL %s | UL %s", formatBytesPerSec(down), formatBytesPerSec(up)))
end

local function parseFans(allText)
    local fanArray = allText:match('"fans"%s*:%s*(%b[])')
    local count = 0

    if fanArray then
        for obj in fanArray:gmatch("%b{}") do
            count = count + 1
            if count > 8 then
                break
            end

            local rawName = stringFrom(obj, "name")
            local rpm = numberFrom(obj, "rpm") or 0
            setVar("Fan" .. count .. "Name", simplifyFanName(rawName, count))
            setVar("Fan" .. count .. "Rpm", string.format("%.0f", rpm))
        end
    end

    for i = count + 1, 8 do
        setVar("Fan" .. i .. "Name", "")
        setVar("Fan" .. i .. "Rpm", "0")
    end
end

function Initialize()
    resetDefaults()
end

function Update()
    local path = SKIN:ReplaceVariables("#MetricsJsonPath#")
    local jsonText = readAll(path)

    resetDefaults()

    if not jsonText then
        setVar("CpuCoreLinesLeft", "metrics.json not found")
        setVar("CpuCoreLinesRight", "")
        setVar("MemoryLine", "metrics.json not found")
        setVar("NetworkLine", "metrics.json not found")
        SKIN:Bang("!UpdateMeterGroup", "DiskRows")
        SKIN:Bang("!Redraw")
        return 0
    end

    parseCpu(jsonText:match('"cpu"%s*:%s*(%b{})'))
    parseGpu(jsonText)
    parseMemory(jsonText:match('"memory"%s*:%s*(%b{})'))
    parseDisks(jsonText)
    parseNetwork(jsonText)
    parseFans(jsonText)

    SKIN:Bang("!UpdateMeterGroup", "DiskRows")
    SKIN:Bang("!Redraw")
    return 1
end
