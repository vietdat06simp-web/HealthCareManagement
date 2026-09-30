1. Phân chia thư mục tĩnh (wwwroot/css/modules/ \& wwwroot/js/modules/):
* Mỗi thành viên khi code giao diện cho module nào chỉ viết trong file CSS và JS của module đó (ví dụ: người làm đơn thuốc chỉ sửa prescriptions.css và prescription-form.js).
* Tránh sửa trực tiếp vào base.css hay common.js trừ khi có thống nhất trước với trưởng nhóm.

2\. Tách Controller và ViewModel:

* Mỗi module sở hữu Controller và thư mục ViewModels/\[TênModule] riêng biệt. Không viết chung Controller để tránh lỗi xung đột merge code trên Git.

3\. Cơ sở dữ liệu (Database):

* Đặt các file script trong thư mục Database/. Khi ai thêm bảng hoặc sửa cột, hãy thêm một file script migrate nhỏ (ví dụ: 20261001\_AddColumn\_BloodSugar.sql) thay vì sửa đè lên file script ban đầu của người khác.

