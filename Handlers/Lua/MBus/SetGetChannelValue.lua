local function Calculate_checksum(body)
	local sum = 0

	for index = 1, #body do
		sum = sum + body[index]
	end
	return sum % 256
end

local function build_long_frame(body)
	local length = #body
	local frame = {
		0x68,
		length,
		length,
		0x68
	}

	for index = 1, #body do
		table.insert(frame, body[index])
	end

	table.insert(frame, Calculate_checksum(body))
	table.insert(frame, 0x16)
	return frame
end

-- GetChannelValue(FC=34, EFC=05)应答;Set 请求不回复
-- 请求帧: 68 L L 68 | 53 FE 51 | 0F | R0 R1 R2 | 34 | 05 | Channel        (Get, 16字节)
-- 应答帧: 68 L L 68 | 08 FE 72 | SN×4 | 49 6A | Gen | Medium | AccessNo | Status | 00 00
--        | 0F | 34(FC) | 05(EFC) | Channel | Value×4(低字节在前) | CS | 16
function build_command_response(request)
	-- Set 请求总长 20 字节(带 4 字节 Value),Get 只有 16 字节
	if #request >= 20 then
		return {}   -- SetChannelValue:按需求不回复
	end

	-- 请求 offset 13 = Channel(lua 数组下标 14)
	local channel = request[14] or 0x01

	-- 你要模拟的通道值,按需修改
	local value = 1234

	local body = {
		0x08, 0xFE, 0x72,             -- C / A / CI
		0x12, 0x34, 0x56, 0x78,       -- SerialNumber(与 GetVersion 应答一致)
		0x49, 0x6A,                   -- Manufacturer = ZRI
		0x09,                         -- Generation
		0x06,                         -- Medium
		0x00,                         -- AccessNumber
		0x00,                         -- Status
		0x00, 0x00,                   -- Signature(未加密)
		0x0F,                         -- DIF = manufacturer specific
		0x34,                         -- FC 回显
		0x05,                         -- EFC 回显
		channel,                      -- Channel 回显(取自请求)
		value % 256,                  -- Value:uint32 低字节在前
		math.floor(value / 256) % 256,
		math.floor(value / 65536) % 256,
		math.floor(value / 16777216) % 256
	}

	return build_long_frame(body)
end
