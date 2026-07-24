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