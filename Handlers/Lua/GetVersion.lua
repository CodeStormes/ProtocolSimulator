local function Caculate_checksum(body)
	local sum = 0

	for index = 1,#body do
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

	for index = 1,#body do
		table.insert(frame,body[index])
	end

	table.insert(frame,Caculate_checksum(body))
	table.insert(frame,0x16)
	return frame
end

function  build_command_request()
	local body = {
		0x53,
		0xFE,
		0x51,
		0x0f,
		0x00,
		0x00,
		0x00,
		0x06
	}

	return build_long_frame(body)
end

-- GetVersion 应答（表计→主站，41 字节，见 ZENNER_DeviceProtocols 4.1.1）
-- 68 23 23 68 | 08(RSP_UD) FE 72 | 序列号BCD×4 | 49 6A(ZRI) | Generation
-- | Medium | AccessNumber | Status | 签名00 00 | 0F | 06(FC 回显)
-- | FirmwareVersion×4 低字节在前 | HardwareIdentification×4 | BuildRevision×4
-- | BuildTime×4 | FirmwareSignature×2 | CS | 16
function  build_command_response(request)
	local body = {
		0x08, 0xFE, 0x72,             -- C / A / CI
		0x12, 0x34, 0x56, 0x78,       -- SerialNumber (BCD)
		0x49, 0x6A,                   -- Manufacturer = ZRI
		0x09,                         -- Generation
		0x06,                         -- Medium (gas)
		0x00,                         -- AccessNumber
		0x00,                         -- Status
		0x00, 0x00,                   -- Signature（未加密）
		0x0F,                         -- DIF
		0x06,                         -- FC
		0x09, 0x30, 0x01, 0x04,       -- FirmwareVersion 0x04013009
		0x00, 0x00, 0x00, 0x00,       -- HardwareIdentification
		0x00, 0x00, 0x00, 0x00,       -- BuildRevision
		0x00, 0x00, 0x00, 0x00,       -- BuildTime
		0x00, 0x00                    -- FirmwareSignature
	}

	return build_long_frame(body)
end