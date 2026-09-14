-- ================================================================
-- Handlers/Lua/NbIot_FUOTA/Response_AckForSetup.lua
-- 场景：DiffGeneratorService 下发 FUOTA Setup(0x0F / CommandID=0x02)
--       设备回复 FunctionCode=0x8F + Tag8(ResultCode=0x00 Success)
-- 依据：Zenner FUOTA 3.1.2.1.2 + Zenner.Protocols.NbIot_FUOTA.NbIot.NbIotPacketParser
-- ================================================================

-- ---------- 设备身份：必须与 DiffGeneratorService.FirmwarePackages.DeviceType 对齐 ----------
local PROTOCOL_VERSION = 0x04
local IMEI             = { 0x84, 0x17, 0x93, 0x13, 0x03, 0x26, 0x77, 0x86 } -- 867726031391784
local DEVICE_MEDIUM    = 0x07                       -- WATER
local MANUFACTURER     = { 0x49, 0x6A }             -- 0x6A49 = ZRI
local GENERATION       = 0x09
local SERIAL_NUMBER    = { 0x12, 0x34, 0x56, 0x78 } -- uint32 LE
local FIRMWARE_VERSION = { 0x09, 0x30, 0x01, 0x04 } -- 0x04013009 -> DeviceIdentity=0x009

-- ---------- 无线信息 ----------
local RSRP    = 0x31
local SNR     = { 0xF8, 0xFF }                      -- int16 LE = -8
local ECL     = 0x01
local CELL_ID = { 0x24, 0x57, 0x56, 0x98, 0x00 }

-- ---------- FUOTA ----------
local FC_FUOTA_RESPONSE = 0x8F
local TAG_RESULT        = 0x08
local CHANNEL_ID        = 0x00
local RESULT_SUCCESS    = 0x00

local MID_FALLBACK = 0x0000   -- 若无法从请求中定位 mID，则用此值

-- ---------------- 通用工具 ----------------
local function checksum(t)
    local sum = 0
    for i = 1, #t do sum = sum + t[i] end
    return sum % 256
end

local function push_all(target, source)
    for i = 1, #source do table.insert(target, source[i]) end
end

local function push_u16_le(target, value)
    table.insert(target, value % 256)
    table.insert(target, math.floor(value / 256) % 256)
end

-- 按 NbIotPacketParser 的期望布局构造 NB-IoT 上行包
local function build_nb_uplink(tlv, mid)
    local data_block = { 0x00 }        -- PaddingLength = 0
    push_all(data_block, tlv)

    local p = { 0x68, PROTOCOL_VERSION }
    push_all(p, IMEI)
    table.insert(p, DEVICE_MEDIUM)
    push_all(p, MANUFACTURER)
    table.insert(p, GENERATION)
    push_all(p, SERIAL_NUMBER)
    push_all(p, FIRMWARE_VERSION)
    table.insert(p, FC_FUOTA_RESPONSE) -- offset 22
    table.insert(p, RSRP)              -- offset 23
    push_all(p, SNR)                   -- offset 24..25
    table.insert(p, ECL)               -- offset 26
    push_all(p, CELL_ID)               -- offset 27..31
    table.insert(p, 0x00)              -- offset 32 CurrentlyUnused
    table.insert(p, 0x00)              -- offset 33 PacketFlag: 无加密 + NoMore
    table.insert(p, 0x00)              -- offset 34 NumberOfAdditionalChannels
    push_u16_le(p, #data_block)        -- offset 35..36 DataLength
    push_all(p, data_block)            -- offset 37..
    push_u16_le(p, mid)                -- mID
    table.insert(p, checksum(p))       -- CheckSum
    table.insert(p, 0x16)              -- End
    return p
end

-- Tag8：Tag + Length(=ResultCode 及其后数据长度) + ChannelID + ResultCode
local function build_result_tlv(result_code)
    return { TAG_RESULT, 0x01, CHANNEL_ID, result_code }
end

-- 在请求帧里定位 NB-IoT 下行包（下行包也是 68...16），取回 CommandID / DataLength / mID
-- 返回：command_id, mid；定位失败返回 nil, mid
local function probe_request(request)
    local size = #request
    for offset = 1, size - 23 do
        if request[offset] == 0x68 and request[offset + 12] == 0x0F then
            local data_len = request[offset + 15] + request[offset + 16] * 256
            local mid_hi_index = offset + 16 + data_len + 1

            if mid_hi_index + 1 <= size and request[offset + 22] ~= nil then
                local mid = request[mid_hi_index] + request[mid_hi_index + 1] * 256
                return request[offset + 22], mid
            end
        end
    end

    return nil, MID_FALLBACK
end

-- 外层封装：MBus 长帧 68 L L 68 <body> CS 16
local function wrap_long_frame(body)
    local frame = { 0x68, #body, #body, 0x68 }
    push_all(frame, body)
    table.insert(frame, checksum(body))
    table.insert(frame, 0x16)
    return frame
end

-- ---------------- 入口 ----------------
function build_command_response(request)
    local command_id, mid = probe_request(request)

    -- 只处理 Setup(0x02)；其它命令交给对应的脚本
    if command_id ~= nil and command_id ~= 0x02 then
        error(string.format("Response_AckForSetup 收到 CommandID=0x%02X，请切换到对应脚本。", command_id))
    end

    local nb_packet = build_nb_uplink(build_result_tlv(RESULT_SUCCESS), mid)
    return wrap_long_frame(nb_packet)
end

local FC_PERIODIC_RESPONSE = 0x82
local PACKET_FLAG = 0x80   -- 0x80=HasMore（服务端随后还要下发 FUOTA）, 0x00=NoMore

-- 空 DataBlock：仍需 1 字节 PaddingLength，保证 DataLength >= 1
local function build_periodic_ack(mid)
    return build_nb_uplink_with({ 0x00 }, FC_PERIODIC_RESPONSE, PACKET_FLAG, mid)
end

function build_command_response(request)
    local _, mid = probe_request(request)
    local nb_packet = build_periodic_ack(mid)
    return wrap_long_frame(nb_packet)
end