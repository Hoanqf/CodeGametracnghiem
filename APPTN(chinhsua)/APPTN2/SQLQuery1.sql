CREATE TABLE dbo.Questions(
Id INT PRIMARY KEY,
QuestionText NVARCHAR(MAX),
OptionA NVARCHAR(MAX),
OptionB NVARCHAR(MAX),
OptionC NVARCHAR(MAX),
OptionD NVARCHAR(MAX),
CorrectAnswer NVARCHAR(1),--Assuming A,B,C,D as single characters
TimeLimit INT
);
-- Thêm câu hỏi 
INSERT INTO dbo.Questions(Id, QuestionText, OptionA, OptionB, OptionC, OptionD, CorrectAnswer, TimeLimit) VALUES
(1, N'Thủ đô của Việt Nam là gì?', N'Hà Nội', N'Hồ Chí Minh', N'Đà Nẵng', N'Hải Phòng', 'A', 15),
(2, N'Người sáng lập Microsoft là ai?', N'Bill Gates', N'Steve Jobs', N'Elon Musk', N'Mark Zuckerberg', 'A', 15),
(3, N'Đâu là hành tinh lớn nhất trong hệ mặt trời?', N'Terra', N'Mars', N'Jupiter', N'Saturn', 'C', 15),
(4, N'Ngọn núi cao nhất thế giới là gì?', N'K2', N'Everest', N'Mount Fuji', N'Aconcagua', 'B', 15),
(5, N'Quốc gia nào có diện tích lớn nhất?', N'Nga', N'Canada', N'Trung Quốc', N'Mỹ', 'A', 15),
(6, N'Ngôn ngữ nào được nói nhiều nhất thế giới?', N'Anh', N'Tây Ban Nha', N'Mandarin', N'Hindi', 'C', 15),
(7, N'Tên gọi khác của Đại Tây Dương là gì?', N'Hai Bà Trưng', N'Thuỷ Vân', N'Đại Dương', N'Atlantis', 'C', 15),
(8, N'Sinh vật nào có thể sống lâu nhất?', N'Tắc kè', N'Rùa', N'Voi', N'Hổ', 'B', 15),
(9, N'Ngày Quốc khánh của Việt Nam là ngày nào?', N'2/9', N'1/1', N'30/4', N'10/3', 'A', 15),
(10, N'Nhà văn nào đã viết "Những ngày thơ ấu"?', N'Tô Hoài', N'Nam Cao', N'Nguyên Hồng', N'Sóng Hồng', 'C', 15),
(11, N'Thành phần chính của không khí là gì?', N'Oxy', N'Hydro', N'Nitơ', N'CO2', 'C', 15),
(12, N'Dãy núi nào dài nhất thế giới?', N'Andes', N'Himalaya', N'Alps', N'Rocky', 'A', 15),
(13, N'Trình duyệt web nào được phát triển bởi Google?', N'Edge', N'Firefox', N'Chrome', N'Safari', 'C', 15),
(14, N'Năm nào diễn ra Cách mạng tháng Tám ở Việt Nam?', N'1940', N'1941', N'1945', N'1954', 'C', 15),
(15, N'Đơn vị đo năng lượng là gì?', N'Watt', N'Volt', N'Ampere', N'Joule', 'D', 15),
(16, N'Máy tính đầu tiên trên thế giới tên là gì?', N'ENIAC', N'Windows 95', N'Macintosh', N'IBM 360', 'A', 15),
(17, N'Loài chim nào không biết bay?', N'Chim én', N'Chim cánh cụt', N'Chim bói cá', N'Chim cú', 'B', 15),
(18, N'Thành phố nào là trung tâm kinh tế lớn nhất Việt Nam?', N'Huế', N'Đà Nẵng', N'Hà Nội', N'Hồ Chí Minh', 'D', 15),
(19, N'Chất nào trong máu giúp vận chuyển oxy?', N'Canxi', N'Hemoglobin', N'Protein', N'Sắt', 'B', 15),
(20, N'Bộ phim hoạt hình nổi tiếng của Nhật Bản do Ghibli sản xuất?', N'Dragon Ball', N'Nausicaa', N'Totoro', N'Doraemon', 'C', 15)
 DROP TABLE dbo.Questions