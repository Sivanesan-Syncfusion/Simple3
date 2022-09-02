var fileUrl = "";

$(document).ready(function () {
    $(document).on("click", "#list-item-nav", function () {
        $('#list-item-nav').removeClass("show").addClass("hide");
        $('#upload-nav').removeClass("hide").addClass("show");
    });

    $(document).on("change", "#files", function () {
        $('#file-upload').removeAttr("disabled");

    });

    $(document).on("click", "#upload-copy-link", function () {
        fnCopyClientCredentials("#upload-copy-link", fileUrl)
    });
});

function uploadFiles(inputId) {
    var input = document.getElementById(inputId);
    var files = input.files;
    key = document.getElementById("key-space").value;
    var formData = new FormData();
    var uploadData = "";
    for (var i = 0; i != files.length; i++) {
        formData.append("files", files[i]);
    }
    formData.append("key", key);
    $("#upload-status").removeClass("hide").addClass("show");
    $('#file-upload').prop('disabled', true);
    $('#files').prop('disabled', true);
    $('#key-space').prop('disabled', true);
    startUpdatingProgressIndicator();

    $.ajax({
        url: '/upload-file',
        type: 'POST',
        data: formData,
        cache: false,
        contentType: false,
        processData: false,
        success: function (result) {
            if (result.status) {
                stopUpdatingProgressIndicator();
                $('#post-upload-dialog').modal('handleUpdate')
                $("#upload-status").removeClass("show").addClass("hide");
                uploadData = `<b>Customer:</b> ${result.customerName}<br/><br/>` +
                    `<b>File:</b> <a href='${result.fileUrl}'>${result.fileName}</a>`;
                fileUrl = result.fileUrl;
                $("#upload-data").html(uploadData)
                $("#post-upload-btn").click();
                $('#file-upload').prop('disabled', true);
                $('#files').prop('disabled', false);
                $('#files').val("");
                $('#key-space').prop('disabled', false);
                $('#key-space').val("");
            }
            else {
                $("#upload-status").removeClass("show").addClass("hide");
                $('#files').val("");
                cuteToast({
                    type: "error",
                    message: "File Upload Failed",
                    timer: 5000
                })
            }            
        }
    });

    var intervalId;
    function startUpdatingProgressIndicator() {
        $("#upload-status").show();
        intervalId = setInterval(
            function () {
                $.post(
                    "/send-progress",
                    function (progress) {
                        $("#upload-status").html(`${progress}`);
                        //if (progress != "100") {
                        //    $("#upload-status").html(`Uploading... ${progress}%`);
                        //}
                    }
                );
            },
            100
        );
    }
    function stopUpdatingProgressIndicator() {
        clearInterval(intervalId);
    }
}

function fnCopyClientCredentials(buttonId, url) {
    navigator.clipboard.writeText(url)
    setTimeout(function () {
        $(buttonId).attr("data-original-title", "Copied");
        $(buttonId).tooltip('show');
    }, 200);
    setTimeout(function () {
        $(buttonId).attr("data-original-title", "Click to copy");
        $(buttonId).tooltip();
    }, 3000);
}