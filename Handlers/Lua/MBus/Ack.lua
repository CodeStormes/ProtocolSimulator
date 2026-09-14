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

-- SetChannelValue(FC=34, EFC=05) 的 ACK 应答
-- 68 13 13 68 | 08 FE 72 | 序列号BCD×4 | 49 6A | Gen | Medium | AccessNo
-- | Status | 签名00 00 | 0F(DIF) FE(ACK code) 34(FC) 05(EFC) | CS | 16
function build_command_response(request)
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
		0xFE,                         -- ACK code(关键,别漏)
		0x34,                         -- FC 回显
		0x05                          -- EFC 回显
	}

	return build_long_frame(body)
end