local jsonText = ""

local function readAll(path)
    local f = io.open(path, "r")
    if not f then
        return nil
    end

    local content = f:read("*a")
    f:close()
    return content
end

local function toNumber(raw)
    if not raw or raw == "null" then
        return nil
    end

    return tonumber(raw)
end

local function numberFrom(block, key)
    if not block then
        return nil
    end

    local value = block:match('"' .. key .. '"%s*:%s*([%-]?%d+%.?%d*)')
    if value then
        return tonumber(value)
    end

    if block:match('"' .. key .. '"%s*:%s*null') then
        return nil
    end

    return nil
end

local function stringFrom(block, key)
    if not block then
        return nil
    end

    return block:match('"' .. key .. '"%s*:%s*"(.-)"')
end

local function formatNumber(value, decimals, suffix)
    if value == nil then
        return "N/A"
    end

    local fmt = "%0." .. tostring(decimals or 0) .. "f"
    local rendered = string.format(fmt, value)
    if suffix then
        return rendered .. suffix
    end

    return rendered
end

local function parseNamedValueArray(arrayText)
    local items = {}
    if not arrayText then
        return items
    end

    for item in arrayText:gmatch("%b{}") do
        local name = stringFrom(item, "name") or "Unknown"
        local value = numberFrom(item, "value")
        table.insert(items, { name = name, value = value })
    end

    return items
end

local function setVar(name, value)
    SKIN:Bang("!SetVariable", name, tostring(value or ""))
end

local function parseCpu(cpuBlock)
    local usage = numberFrom(cpuBlock, "usageTotalPercent")
    local temp = numberFrom(cpuBlock, "temperatureC")

    setVar("CpuUsage", formatNumber(usage, 0, "%"))
    setVar("CpuTemp", formatNumber(temp, 0, "°C"))

    local coreUsageArray = cpuBlock and cpuBlock:match('"perCoreUsagePercent"%s*:%s*(%b[])')
    local coreTempArray = cpuBlock and cpuBlock:match('"coreTemperaturesC"%s*:%s*(%b[])')

    local usages = parseNamedValueArray(coreUsageArray)
    local temps = parseNamedValueArray(coreTempArray)
    local tempByName = {}

    for _, t in ipairs(temps) do
        tempByName[t.name] = t.value
    end

    local lines = {}
    for i, c in ipairs(usages) do
        local t = tempByName[c.name]
        table.insert(lines, string.format("%s: %s / %s", c.name, formatNumber(c.value, 0, "%"), formatNumber(t, 0, "°C")))
        if i >= 24 then
            break
        end
    end

    if #lines == 0 then
        table.insert(lines, "No per-core usage metrics")
    end

    setVar("CpuCoreLines", table.concat(lines, "#CRLF#"))
end

local function parseGpu(allText)
    local gpuArray = allText:match('"gpu"%s*:%s*(%b[])')
    local targetName = "NVIDIA GeForce RTX 4080 SUPER"
    local selected = nil

    local fallback = nil
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

    if not selected then
        selected = fallback
    end

    if not selected then
        setVar("GpuName", targetName)
        setVar("GpuUsage", "N/A")
        setVar("GpuTemp", "N/A")
        return
    end

    local usage = numberFrom(selected, "coreUsagePercent")
    local temp = numberFrom(selected, "temperatureC")

    setVar("GpuName", stringFrom(selected, "name") or targetName)
    setVar("GpuUsage", formatNumber(usage, 0, "%"))
    setVar("GpuTemp", formatNumber(temp, 0, "°C"))
end

local function parseMemory(memBlock)
    local total = numberFrom(memBlock, "totalMB")
    local used = numberFrom(memBlock, "usedMB")
    local available = numberFrom(memBlock, "availableMB")
    local percent = numberFrom(memBlock, "usagePercent")

    setVar("MemoryLine", string.format(
        "Total %s GB | Used %s GB | Available %s GB | %s",
        formatNumber(total and (total / 1024), 1, ""),
        formatNumber(used and (used / 1024), 1, ""),
        formatNumber(available and (available / 1024), 1, ""),
        formatNumber(percent, 0, "%")
    ))
end

local function parseDisks(allText)
    local diskArray = allText:match('"disks"%s*:%s*(%b[])')
    local lines = {}

    if diskArray then
        for obj in diskArray:gmatch("%b{}") do
            local name = stringFrom(obj, "name") or stringFrom(obj, "mountPoint") or "Disk"
            local total = numberFrom(obj, "totalGB")
            local used = numberFrom(obj, "usedGB")
            local free = numberFrom(obj, "freeGB")
            local percent = numberFrom(obj, "usagePercent")

            table.insert(lines, string.format(
                "%s: T %s | U %s | F %s | %s",
                name,
                formatNumber(total, 0, "GB"),
                formatNumber(used, 0, "GB"),
                formatNumber(free, 0, "GB"),
                formatNumber(percent, 0, "%")
            ))
        end
    end

    if #lines == 0 then
        table.insert(lines, "No disk metrics")
    end

    setVar("DiskLines", table.concat(lines, "#CRLF#"))
end

local function parseNetwork(allText)
    local netArray = allText:match('"network"%s*:%s*(%b[])')
    local selected = nil

    if netArray then
        for obj in netArray:gmatch("%b{}") do
            local name = stringFrom(obj, "name")
            if name == "Ethernet" then
                selected = obj
                break
            end
        end
    end

    local up, down
    if selected then
        up = numberFrom(selected, "bytesSentPerSec")
        down = numberFrom(selected, "bytesReceivedPerSec")
    end

    setVar("NetworkLine", string.format("Ethernet: ↓ %s/s | ↑ %s/s", formatNumber(down, 0, "B"), formatNumber(up, 0, "B")))
end

local function parseFans(allText)
    local fanArray = allText:match('"fans"%s*:%s*(%b[])')
    local idx = 0

    if fanArray then
        for obj in fanArray:gmatch("%b{}") do
            idx = idx + 1
            if idx > 8 then
                break
            end

            local name = stringFrom(obj, "name") or ("Fan " .. tostring(idx))
            local rpm = numberFrom(obj, "rpm") or 0
            setVar("Fan" .. idx .. "Name", name)
            setVar("Fan" .. idx .. "Rpm", string.format("%0.0f", rpm))
        end
    end

    for i = idx + 1, 8 do
        setVar("Fan" .. i .. "Name", "")
        setVar("Fan" .. i .. "Rpm", "0")
    end
end

function Initialize()
    setVar("CpuUsage", "N/A")
    setVar("CpuTemp", "N/A")
    setVar("CpuCoreLines", "Loading...")
    setVar("GpuName", "NVIDIA GeForce RTX 4080 SUPER")
    setVar("GpuUsage", "N/A")
    setVar("GpuTemp", "N/A")
    setVar("MemoryLine", "Loading...")
    setVar("DiskLines", "Loading...")
    setVar("NetworkLine", "Loading...")
    for i = 1, 8 do
        setVar("Fan" .. i .. "Name", "")
        setVar("Fan" .. i .. "Rpm", "0")
    end
end

function Update()
    local path = SKIN:ReplaceVariables("#MetricsJsonPath#")
    jsonText = readAll(path)
    if not jsonText then
        setVar("CpuCoreLines", "metrics.json not found")
        setVar("MemoryLine", "metrics.json not found")
        setVar("DiskLines", "metrics.json not found")
        setVar("NetworkLine", "metrics.json not found")
        return 0
    end

    local cpuBlock = jsonText:match('"cpu"%s*:%s*(%b{})')
    local memoryBlock = jsonText:match('"memory"%s*:%s*(%b{})')

    parseCpu(cpuBlock)
    parseGpu(jsonText)
    parseMemory(memoryBlock)
    parseDisks(jsonText)
    parseNetwork(jsonText)
    parseFans(jsonText)

    return 1
end
